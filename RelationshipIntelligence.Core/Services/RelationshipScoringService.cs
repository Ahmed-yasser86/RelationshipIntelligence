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

        public RelationshipScoringService(
            PersonRepositryContract persons,
            RelationshipStateRepositoryContract states,
            ICurrentUserService currentUser,
            IUnitOfWork unitOfWork,
            ILogger<RelationshipScoringService> logger)
        {
            _persons = persons;
            _states = states;
            _currentUser = currentUser;
            _unitOfWork = unitOfWork;
            _logger = logger;
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
                    await _states.UpsertAsync(BuildState(
                        ownerId, person!, strengths[person!.PersonId], max, now));
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

            var persons = (await _persons.GetAllPersons()).Where(p => p != null).ToList();
            var now = DateTime.UtcNow;
            double max = persons
                .Select(p => TieDecayModel.StrengthAt(
                    p!.Interactions
                        .OrderBy(i => i.TimeOfInteraction)
                        .Select(i => i.TimeOfInteraction)
                        .ToList(),
                    now))
                .DefaultIfEmpty(1.0).Max();
            if (max <= 0) max = 1.0;

            double strength = TieDecayModel.StrengthAt(
                person.Interactions
                    .OrderBy(i => i.TimeOfInteraction)
                    .Select(i => i.TimeOfInteraction)
                    .ToList(),
                now);
            max = Math.Max(max, strength);

            await _states.UpsertAsync(BuildState(userId.Value, person, strength, max, now));
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<List<RelationshipHealthResponse>> GetQueueAsync(int top = 7)
        {
            var userId = _currentUser.UserId;
            if (userId == null || userId == Guid.Empty)
                return new List<RelationshipHealthResponse>();

            var persons = (await _persons.GetAllPersons())
                .Where(p => p != null && !IsExcluded(p!))
                .ToDictionary(p => p!.PersonId);
            var states = await _states.ListForOwnerAsync(userId.Value);

            return states
                .Where(s => persons.ContainsKey(s.PersonId))
                .OrderByDescending(s => s.UrgencyScore)
                .ThenByDescending(s => persons[s.PersonId].SystemStatusTags != null
                    && persons[s.PersonId].SystemStatusTags.Any(t =>
                        t.StatusTagId == EnSystemStatusTag.HighPriority
                        || t.StatusTagId == EnSystemStatusTag.Urgent))
                .ThenBy(s => s.LastContactAtUtc ?? DateTime.MinValue)
                .Take(top <= 0 ? 7 : top)
                .Select(s => new RelationshipHealthResponse
                {
                    PersonId = s.PersonId,
                    Name = persons[s.PersonId].Name,
                    TieStrength = s.TieStrength,
                    LastContactAtUtc = s.LastContactAtUtc,
                    CadenceReferenceDays = s.CadenceReferenceDays,
                    UrgencyScore = s.UrgencyScore,
                    Band = TieDecayModel.BandFor(s.UrgencyScore).ToString(),
                    IsBridge = s.IsBridge,
                    IsImportant = persons[s.PersonId].SystemStatusTags != null
                        && persons[s.PersonId].SystemStatusTags.Any(t =>
                            t.StatusTagId == EnSystemStatusTag.HighPriority
                            || t.StatusTagId == EnSystemStatusTag.Urgent)
                })
                .ToList();
        }

        private static bool IsExcluded(Person person)
        {
            return person.UserDefinedTags != null
                && person.UserDefinedTags.Any(t =>
                    string.Equals(t.TagName, "NoScore", StringComparison.OrdinalIgnoreCase));
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

            return new RelationshipState
            {
                ApplicationUserId = ownerId,
                PersonId = person.PersonId,
                TieStrength = strength,
                LastContactAtUtc = last,
                CadenceReferenceDays = gaps.Count == 0 ? prior : TieDecayModel.CadenceReference(gaps, prior),
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
