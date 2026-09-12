using ContactsManger.Core.Domain.Entities.EEnums;
using Entities;
using Microsoft.Extensions.Logging;
using RepositryContracts;
using SerilogTimings;
using ServiceContracts;
using ServiceContracts.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Servicess
{
    public class RelationshipScoringService : IRelationshipScoringService
    {
        private readonly PersonRepositryContract _persons;
        private readonly RelationshipStateRepositoryContract _states;
        private readonly ICurrentUserService _currentUser;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RelationshipScoringService> _logger;
        private readonly IEventService? _events;

        public RelationshipScoringService(
            PersonRepositryContract persons,
            RelationshipStateRepositoryContract states,
            ICurrentUserService currentUser,
            IUnitOfWork unitOfWork,
            ILogger<RelationshipScoringService> logger,
            IEventService? eventService = null)
        {
            _persons = persons;
            _states = states;
            _currentUser = currentUser;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _events = eventService;
        }

        public async Task<int> RecomputeForCurrentUserAsync()
        {
            var userId = _currentUser.UserId;
            if (userId == null || userId == Guid.Empty)
                throw new UnauthorizedAccessException("Cannot score relationships without an authenticated user.");

            return await RecomputeForOwnerAsync(userId.Value);
        }

        public async Task<int> RecomputeForOwnerAsync(Guid ownerId)
        {
            using (Operation.Time("Recompute relationship states"))
            {
                if (ownerId == Guid.Empty)
                    return 0;

                var persons = (await _persons.GetAllPersons())
                    .Where(p => p != null && !IsExcluded(p!))
                    .ToList();
                var now = DateTime.UtcNow;

                var strengths = persons.ToDictionary(
                    p => p!.PersonId,
                    p => TieDecayModel.StrengthAt(
                        p!.Interactions
                            .OrderBy(i => i.TimeOfInteraction)
                            .Select(i => i.TimeOfInteraction)
                            .ToList(),
                        now));
                double max = strengths.Values.DefaultIfEmpty(1.0).Max();
                if (max <= 0) max = 1.0;

                foreach (var person in persons)
                {
                    var state = BuildState(
                        ownerId, person!, strengths[person!.PersonId], max, now);
                    await _states.UpsertAsync(state);
                    await AppendSnapshotIfNewDayAsync(ownerId, person!.PersonId, state, now);
                }
                await _unitOfWork.SaveChangesAsync();

                return persons.Count;
            }
        }

        public async Task RecomputeForPairAsync(Guid? personId)
        {
            var userId = _currentUser.UserId;
            if (userId == null || userId == Guid.Empty)
                throw new UnauthorizedAccessException("Cannot score relationships without an authenticated user.");

            var person = await _persons.GetPersonById(personId);
            if (person == null || IsExcluded(person))
                return;

            var now = DateTime.UtcNow;
            double strength = TieDecayModel.StrengthAt(
                person.Interactions
                    .OrderBy(i => i.TimeOfInteraction)
                    .Select(i => i.TimeOfInteraction)
                    .ToList(),
                now);

            var states = await _states.ListForOwnerAsync(userId.Value);
            double max = states.Count == 0
                ? 1.0
                : Math.Max(states.Max(s => s.TieStrength), strength);
            if (max <= 0) max = 1.0;

            var state = BuildState(userId.Value, person, strength, max, now);
            await _states.UpsertAsync(state);
            await AppendSnapshotIfNewDayAsync(userId.Value, person.PersonId, state, now);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<int> RecomputePairsAsync(IEnumerable<Guid> personIds)
        {
            var userId = _currentUser.UserId;
            if (userId == null || userId == Guid.Empty)
                throw new UnauthorizedAccessException("Cannot score relationships without an authenticated user.");

            var ids = personIds?.ToHashSet() ?? new HashSet<Guid>();
            if (ids.Count == 0)
                return 0;

            var persons = await _persons.ListByIdsAsync(ids);
            var states = await _states.ListForOwnerAsync(userId.Value);
            var now = DateTime.UtcNow;
            double max = states.Count == 0 ? 1.0 : states.Max(s => s.TieStrength);
            if (max <= 0) max = 1.0;

            int count = 0;
            foreach (var person in persons.Where(p => p != null && !IsExcluded(p!)))
            {
                double strength = TieDecayModel.StrengthAt(
                    person!.Interactions
                        .OrderBy(i => i.TimeOfInteraction)
                        .Select(i => i.TimeOfInteraction)
                        .ToList(),
                    now);
                max = Math.Max(max, strength);
                var state = BuildState(userId.Value, person, strength, max, now);
                await _states.UpsertAsync(state);
                await AppendSnapshotIfNewDayAsync(userId.Value, person.PersonId, state, now);
                count++;
            }
            await _unitOfWork.SaveChangesAsync();
            return count;
        }

        public async Task<RelationshipStateHistoryResponse> GetHistoryAsync(Guid? personId)
        {
            var userId = _currentUser.UserId;
            var response = new RelationshipStateHistoryResponse();
            if (userId == null || userId == Guid.Empty || personId == null)
                return response;

            var person = await _persons.GetPersonById(personId);
            if (person == null)
                return response;

            response.PersonId = person.PersonId;
            var snapshots = await _states.ListSnapshotsAsync(userId.Value, person.PersonId);
            response.Points = snapshots
                .Select(s => new RelationshipStateHistoryPoint
                {
                    TakenAtUtc = s.TakenAtUtc,
                    TieStrength = s.TieStrength,
                    UrgencyScore = s.UrgencyScore,
                    Band = s.Band
                })
                .ToList();
            return response;
        }

        private async Task AppendSnapshotIfNewDayAsync(
            Guid ownerId, Guid personId, RelationshipState state, DateTime now)
        {
            // History without evidence is noise: never snapshot unscored rows.
            if (state.EvidenceStatus == EvidenceStatus.NoHistory)
                return;

            var existing = await _states.ListSnapshotsAsync(ownerId, personId);
            if (existing.Any(s => s.TakenAtUtc.Date == now.Date))
                return;

            await _states.AddSnapshotAsync(new RelationshipStateSnapshot
            {
                ApplicationUserId = ownerId,
                PersonId = personId,
                TakenAtUtc = now,
                TieStrength = state.TieStrength,
                UrgencyScore = state.UrgencyScore,
                Band = TieDecayModel.BandFor(state.UrgencyScore).ToString()
            });
        }

        public async Task<List<RelationshipHealthResponse>> GetQueueAsync(int top = 7)
        {
            var userId = _currentUser.UserId;
            if (userId == null || userId == Guid.Empty)
                return new List<RelationshipHealthResponse>();

            var people = (await _persons.ListAffinitiesAsync())
                .Where(p => p != null && !IsExcluded(p!))
                .ToDictionary(p => p!.PersonId);
            var states = await _states.ListForOwnerAsync(userId.Value);

            var queue = states
                .Where(s => people.ContainsKey(s.PersonId))
                .Where(s => s.EvidenceStatus != EvidenceStatus.NoHistory)
                .OrderByDescending(s => s.UrgencyScore)
                .ThenByDescending(s => IsImportant(people[s.PersonId]))
                .ThenBy(s => s.LastContactAtUtc ?? DateTime.MinValue)
                .Take(top <= 0 ? 7 : top)
                .Select(s => new RelationshipHealthResponse
                {
                    PersonId = s.PersonId,
                    Name = people[s.PersonId].Name ?? string.Empty,
                    TieStrength = s.TieStrength,
                    LastContactAtUtc = s.LastContactAtUtc,
                    InteractionCount = s.InteractionCount,
                    EvidenceStatus = s.EvidenceStatus.ToString(),
                    CadenceReferenceDays = s.CadenceReferenceDays,
                    SilenceQuantile = s.SilenceQuantile,
                    UrgencyScore = s.UrgencyScore,
                    Band = TieDecayModel.BandFor(s.UrgencyScore).ToString(),
                    IsBridge = s.IsBridge,
                    IsImportant = IsImportant(people[s.PersonId])
                })
                .ToList();

            await EnrichWithUpcomingEventsAsync(queue);
            return queue;
        }

        /// <summary>
        /// Attaches upcoming event occurrences to queue rows. Enrichment only:
        /// scores, bands, and ordering are never changed here.
        /// </summary>
        private async Task EnrichWithUpcomingEventsAsync(List<RelationshipHealthResponse> queue)
        {
            if (_events == null || queue.Count == 0)
                return;

            var now = DateTime.UtcNow;
            var occurrences = await _events.GetUpcomingAsync(21);
            var byPerson = occurrences.GroupBy(o => o.PersonId).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var row in queue)
            {
                if (!byPerson.TryGetValue(row.PersonId, out var personEvents))
                    continue;

                row.UpcomingEvents = personEvents;
                var silenceDays = row.LastContactAtUtc == null
                    ? (double?)null
                    : (now - row.LastContactAtUtc.Value).TotalDays;
                row.HasEventSignal = personEvents.Any(e => e.InDays <= 7)
                    && (row.UrgencyScore > 65
                        || (silenceDays != null && row.CadenceReferenceDays != null && silenceDays > row.CadenceReferenceDays));
            }
        }

        private static bool IsImportant(PersonAffinity person)
        {
            return person.SystemTagNames.Any(t =>
                string.Equals(t, nameof(EnSystemStatusTag.HighPriority), StringComparison.OrdinalIgnoreCase)
                || string.Equals(t, nameof(EnSystemStatusTag.Urgent), StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsExcluded(Person person)
        {
            return person.UserDefinedTags != null
                && person.UserDefinedTags.Any(t =>
                    string.Equals(t.TagName, "NoScore", StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsExcluded(PersonAffinity person)
        {
            return person.TagNames.Any(t =>
                string.Equals(t, "NoScore", StringComparison.OrdinalIgnoreCase));
        }

        private static RelationshipState BuildState(
            Guid ownerId, Person person, double strength, double maxStrength, DateTime now)
        {
            var times = person.Interactions
                .Select(i => i.TimeOfInteraction.ToUniversalTime())
                .OrderBy(t => t)
                .ToList();
            var gaps = times.Zip(times.Skip(1), (a, b) => (b - a).TotalDays).ToList();
            double prior = PersonaPriors.DaysFor(
                person.ContactItemRoles?.Select(r => r.Role));

            DateTime? last = times.Count == 0 ? null : (DateTime?)times[^1];

            // No observed events means no evidence of any kind: no cadence can be
            // inferred (not even from priors - a prior describes an expected rhythm,
            // and there is no relationship rhythm to describe yet), no quantile can
            // be computed, and urgency must stay neutral. Such rows are excluded
            // from every ranked surface; see GetQueueAsync.
            if (times.Count == 0)
            {
                return new RelationshipState
                {
                    ApplicationUserId = ownerId,
                    PersonId = person.PersonId,
                    TieStrength = 0,
                    LastContactAtUtc = null,
                    InteractionCount = 0,
                    EvidenceStatus = EvidenceStatus.NoHistory,
                    CadenceReferenceDays = null,
                    SilenceQuantile = null,
                    IsBridge = false,
                    UrgencyScore = 0,
                    UpdatedAtUtc = now
                };
            }

            return new RelationshipState
            {
                ApplicationUserId = ownerId,
                PersonId = person.PersonId,
                TieStrength = strength,
                LastContactAtUtc = last,
                InteractionCount = times.Count,
                EvidenceStatus = gaps.Count >= 3
                    ? EvidenceStatus.Established
                    : EvidenceStatus.Insufficient,
                CadenceReferenceDays = gaps.Count == 0 ? null : TieDecayModel.CadenceReference(gaps, prior),
                SilenceQuantile = last == null
                    ? null
                    : TieDecayModel.SilenceQuantile(gaps, (now - last.Value).TotalDays),
                IsBridge = false,
                UrgencyScore = TieDecayModel.Urgency(strength, maxStrength),
                UpdatedAtUtc = now
            };
        }
    }
}
