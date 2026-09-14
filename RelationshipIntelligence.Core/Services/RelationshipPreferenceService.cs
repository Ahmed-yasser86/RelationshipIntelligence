using Entities;
using Microsoft.Extensions.Logging;
using RepositryContracts;
using SerilogTimings;
using ServiceContracts;
using ServiceContracts.DTOs.PreferenceDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Servicess
{
    /// <summary>
    /// User-controlled relationship parameters in human terms. Persists intent
    /// (cadence, importance, suggestions, reminders) and translates it into the
    /// deterministic system without ever rewriting the scoring model itself.
    /// </summary>
    public class RelationshipPreferenceService : IRelationshipPreferenceService
    {
        private readonly RelationshipPreferenceRepositoryContract _preferences;
        private readonly PersonRepositryContract _persons;
        private readonly IRelationshipScoringService _scoring;
        private readonly ICurrentUserService _currentUser;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RelationshipPreferenceService> _logger;

        public RelationshipPreferenceService(
            RelationshipPreferenceRepositoryContract preferences,
            PersonRepositryContract persons,
            IRelationshipScoringService scoring,
            ICurrentUserService currentUser,
            IUnitOfWork unitOfWork,
            ILogger<RelationshipPreferenceService> logger)
        {
            _preferences = preferences;
            _persons = persons;
            _scoring = scoring;
            _currentUser = currentUser;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        private Guid OwnerId()
        {
            var id = _currentUser.UserId;
            if (id == null || id == Guid.Empty)
                throw new UnauthorizedAccessException("Preferences require an authenticated user.");
            return id.Value;
        }

        private async Task<Person> RequireOwnedPersonAsync(Guid ownerId, Guid personId)
        {
            var person = await _persons.GetPersonById(personId);
            if (person == null || person.ApplicationUserId != ownerId)
                throw new KeyNotFoundException($"No person found with id '{personId}'.");
            return person;
        }

        private static void ValidateInterval(int days, string name)
        {
            if (days < 1 || days > 365)
                throw new ArgumentException("Interval must be between 1 and 365 days.", name);
        }

        public async Task<RelationshipPreferenceDto?> GetAsync(Guid personId)
        {
            var ownerId = OwnerId();
            await RequireOwnedPersonAsync(ownerId, personId);
            var preference = await _preferences.GetAsync(ownerId, personId);
            if (preference == null)
                return null;
            var person = await _persons.GetPersonById(personId);
            return ToDto(preference, person?.Name);
        }

        public async Task<RelationshipPreferenceDto> SaveAsync(PreferenceSaveRequest request)
        {
            using (Operation.Time("Save relationship preference"))
            {
                if (request == null)
                    throw new ArgumentNullException(nameof(request));
                if (request.DesiredCadenceDays != null)
                    ValidateInterval(request.DesiredCadenceDays.Value, nameof(request.DesiredCadenceDays));
                if (request.Importance != null && (request.Importance < 0 || request.Importance > 1))
                    throw new ArgumentException("Importance must be 0 or 1.", nameof(request.Importance));
                if (request.Priority != null && (request.Priority < 0 || request.Priority > 1))
                    throw new ArgumentException("Priority must be 0 or 1.", nameof(request.Priority));

                var ownerId = OwnerId();
                var person = await RequireOwnedPersonAsync(ownerId, request.PersonId);
                var preference = await _preferences.GetAsync(ownerId, request.PersonId);
                var isNew = preference == null;
                preference ??= new RelationshipPreference
                {
                    RelationshipPreferenceId = Guid.NewGuid(),
                    ApplicationUserId = ownerId,
                    PersonId = request.PersonId,
                    CreatedAtUtc = DateTime.UtcNow
                };
                if (request.DesiredCadenceDays != null)
                    preference.DesiredCadenceDays = request.DesiredCadenceDays;
                if (request.Importance != null)
                    preference.Importance = request.Importance.Value;
                if (request.Priority != null)
                    preference.Priority = request.Priority.Value;
                if (request.KeepInTouchIntentionally != null)
                    preference.KeepInTouchIntentionally = request.KeepInTouchIntentionally.Value;
                if (request.ExcludeFromSuggestions != null)
                    preference.ExcludeFromSuggestions = request.ExcludeFromSuggestions.Value;
                preference.UpdatedAtUtc = DateTime.UtcNow;
                if (isNew)
                    await _preferences.AddAsync(preference);
                await _unitOfWork.SaveChangesAsync();
                return ToDto(preference, person.Name);
            }
        }

        public async Task<RelationshipPreferenceDto> SetReminderAsync(ReminderSetRequest request)
        {
            using (Operation.Time("Set reminder"))
            {
                if (request == null)
                    throw new ArgumentNullException(nameof(request));
                ValidateInterval(request.IntervalDays, nameof(request.IntervalDays));

                var ownerId = OwnerId();
                var person = await RequireOwnedPersonAsync(ownerId, request.PersonId);
                var preference = await _preferences.GetAsync(ownerId, request.PersonId);
                var isNew = preference == null;
                preference ??= new RelationshipPreference
                {
                    RelationshipPreferenceId = Guid.NewGuid(),
                    ApplicationUserId = ownerId,
                    PersonId = request.PersonId,
                    CreatedAtUtc = DateTime.UtcNow
                };
                preference.ReminderEnabled = true;
                preference.ReminderIntervalDays = request.IntervalDays;
                preference.ReminderStrict = request.Strict;
                preference.SnoozedUntilUtc = null;
                preference.UpdatedAtUtc = DateTime.UtcNow;
                if (isNew)
                    await _preferences.AddAsync(preference);
                await _unitOfWork.SaveChangesAsync();
                return ToDto(preference, person.Name);
            }
        }

        public async Task<RelationshipPreferenceDto> SnoozeAsync(Guid personId, int days)
        {
            if (days < 1 || days > 90)
                throw new ArgumentException("Snooze must be between 1 and 90 days.", nameof(days));
            var ownerId = OwnerId();
            var person = await RequireOwnedPersonAsync(ownerId, personId);
            var preference = await _preferences.GetAsync(ownerId, personId)
                ?? throw new KeyNotFoundException("No reminder is configured for this person.");
            preference.SnoozedUntilUtc = DateTime.UtcNow.AddDays(days);
            preference.UpdatedAtUtc = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync();
            return ToDto(preference, person.Name);
        }

        public async Task<RelationshipPreferenceDto> SkipAsync(Guid personId)
        {
            var ownerId = OwnerId();
            var person = await RequireOwnedPersonAsync(ownerId, personId);
            var preference = await _preferences.GetAsync(ownerId, personId)
                ?? throw new KeyNotFoundException("No reminder is configured for this person.");
            preference.LastSkippedAtUtc = DateTime.UtcNow;
            preference.SnoozedUntilUtc = DateTime.UtcNow.AddDays(preference.ReminderIntervalDays ?? 7);
            preference.UpdatedAtUtc = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync();
            return ToDto(preference, person.Name);
        }

        public async Task<RelationshipPreferenceDto> CompleteAsync(Guid personId)
        {
            var ownerId = OwnerId();
            var person = await RequireOwnedPersonAsync(ownerId, personId);
            var preference = await _preferences.GetAsync(ownerId, personId)
                ?? throw new KeyNotFoundException("No reminder is configured for this person.");
            // Completing records the user's action only. Relationship state moves
            // exclusively through logged interactions in the canonical pipeline.
            preference.LastCompletedAtUtc = DateTime.UtcNow;
            preference.SnoozedUntilUtc = null;
            preference.UpdatedAtUtc = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync();
            return ToDto(preference, person.Name);
        }

        public async Task<RelationshipPreferenceDto> DisableReminderAsync(Guid personId)
        {
            var ownerId = OwnerId();
            var person = await RequireOwnedPersonAsync(ownerId, personId);
            var preference = await _preferences.GetAsync(ownerId, personId)
                ?? throw new KeyNotFoundException("No reminder is configured for this person.");
            preference.ReminderEnabled = false;
            preference.SnoozedUntilUtc = null;
            preference.UpdatedAtUtc = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync();
            return ToDto(preference, person.Name);
        }

        public async Task<List<ReminderDueDto>> ListDueAsync()
        {
            using (Operation.Time("List due reminders"))
            {
                var ownerId = OwnerId();
                var now = DateTime.UtcNow;
                var preferences = (await _preferences.ListForOwnerAsync(ownerId))
                    .Where(p => p.ReminderEnabled && p.ReminderIntervalDays != null)
                    .Where(p => p.SnoozedUntilUtc == null || p.SnoozedUntilUtc <= now)
                    .ToList();
                if (preferences.Count == 0)
                    return new List<ReminderDueDto>();

                var queue = await _scoring.GetQueueAsync(200);
                var silenceByPerson = queue.ToDictionary(q => q.PersonId, q => q.SilenceDays);
                var people = await _persons.GetAllPersons();
                var names = people.Where(p => p != null).ToDictionary(p => p!.PersonId, p => p!.Name);

                var due = new List<ReminderDueDto>();
                foreach (var preference in preferences)
                {
                    if (!names.TryGetValue(preference.PersonId, out _))
                        continue;
                    int? silence = silenceByPerson.TryGetValue(preference.PersonId, out var days) ? days : null;
                    // No scored state yet counts as due: the user asked to be reminded.
                    if (silence != null && silence < preference.ReminderIntervalDays)
                        continue;
                    due.Add(new ReminderDueDto
                    {
                        PersonId = preference.PersonId,
                        PersonName = names[preference.PersonId],
                        IntervalDays = preference.ReminderIntervalDays!.Value,
                        Strict = preference.ReminderStrict,
                        SilenceDays = silence,
                        SourceLabel = "You configured this reminder."
                    });
                }
                return due
                    .OrderByDescending(d => d.Strict)
                    .ThenByDescending(d => d.SilenceDays ?? int.MaxValue)
                    .ToList();
            }
        }

        private static RelationshipPreferenceDto ToDto(RelationshipPreference preference, string? name) => new()
        {
            PersonId = preference.PersonId,
            PersonName = name,
            DesiredCadenceDays = preference.DesiredCadenceDays,
            Importance = preference.Importance,
            Priority = preference.Priority,
            KeepInTouchIntentionally = preference.KeepInTouchIntentionally,
            ExcludeFromSuggestions = preference.ExcludeFromSuggestions,
            ReminderEnabled = preference.ReminderEnabled,
            ReminderIntervalDays = preference.ReminderIntervalDays,
            ReminderStrict = preference.ReminderStrict,
            SnoozedUntilUtc = preference.SnoozedUntilUtc,
            LastCompletedAtUtc = preference.LastCompletedAtUtc
        };
    }
}
