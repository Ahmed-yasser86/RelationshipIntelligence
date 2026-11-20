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
        private readonly PreferenceAuditRepositoryContract _audit;
        private readonly GlobalDefaultsRepositoryContract _defaults;
        private readonly ICurrentUserService _currentUser;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RelationshipPreferenceService> _logger;

        public RelationshipPreferenceService(
            RelationshipPreferenceRepositoryContract preferences,
            PersonRepositryContract persons,
            IRelationshipScoringService scoring,
            ICurrentUserService currentUser,
            IUnitOfWork unitOfWork,
            ILogger<RelationshipPreferenceService> logger,
            PreferenceAuditRepositoryContract? audit = null,
            GlobalDefaultsRepositoryContract? defaults = null)
        {
            _preferences = preferences;
            _persons = persons;
            _scoring = scoring;
            _currentUser = currentUser;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _audit = audit!;
            _defaults = defaults!;
        }

        private static string HumanDays(int? days) =>
            days == null ? "not set" : $"every {days} days";

        private static string HumanFlag(bool value) => value ? "on" : "off";

        private static string HumanLevel(int value) => value == 1 ? "high" : "normal";

        private async Task AuditAsync(Guid ownerId, Guid personId, string field, string? previous, string? current, PreferenceChangeSource source)
        {
            if (_audit == null)
                return;
            if (string.Equals(previous, current, StringComparison.Ordinal))
                return;
            await _audit.AddAsync(new PreferenceAuditEntry
            {
                PreferenceAuditEntryId = Guid.NewGuid(),
                ApplicationUserId = ownerId,
                PersonId = personId,
                Field = field,
                PreviousValue = previous,
                NewValue = current,
                Source = source,
                ChangedById = ownerId,
                ChangedAtUtc = DateTime.UtcNow
            });
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

        public async Task<RelationshipPreferenceDto> SaveAsync(PreferenceSaveRequest request, PreferenceChangeSource source = PreferenceChangeSource.User)
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
                {
                    await AuditAsync(ownerId, request.PersonId, nameof(RelationshipPreference.DesiredCadenceDays),
                        HumanDays(preference.DesiredCadenceDays), HumanDays(request.DesiredCadenceDays), source);
                    preference.DesiredCadenceDays = request.DesiredCadenceDays;
                }
                if (request.Importance != null)
                {
                    await AuditAsync(ownerId, request.PersonId, nameof(RelationshipPreference.Importance),
                        HumanLevel(preference.Importance), HumanLevel(request.Importance.Value), source);
                    preference.Importance = request.Importance.Value;
                }
                if (request.Priority != null)
                {
                    await AuditAsync(ownerId, request.PersonId, nameof(RelationshipPreference.Priority),
                        HumanLevel(preference.Priority), HumanLevel(request.Priority.Value), source);
                    preference.Priority = request.Priority.Value;
                }
                if (request.KeepInTouchIntentionally != null)
                {
                    await AuditAsync(ownerId, request.PersonId, nameof(RelationshipPreference.KeepInTouchIntentionally),
                        HumanFlag(preference.KeepInTouchIntentionally), HumanFlag(request.KeepInTouchIntentionally.Value), source);
                    preference.KeepInTouchIntentionally = request.KeepInTouchIntentionally.Value;
                }
                if (request.ExcludeFromSuggestions != null)
                {
                    await AuditAsync(ownerId, request.PersonId, nameof(RelationshipPreference.ExcludeFromSuggestions),
                        HumanFlag(preference.ExcludeFromSuggestions), HumanFlag(request.ExcludeFromSuggestions.Value), source);
                    preference.ExcludeFromSuggestions = request.ExcludeFromSuggestions.Value;
                }
                preference.UpdatedAtUtc = DateTime.UtcNow;
                if (isNew)
                    await _preferences.AddAsync(preference);
                await _unitOfWork.SaveChangesAsync();
                return ToDto(preference, person.Name);
            }
        }

        public async Task<RelationshipPreferenceDto> SetReminderAsync(ReminderSetRequest request, PreferenceChangeSource source = PreferenceChangeSource.User)
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
                await AuditAsync(ownerId, request.PersonId, nameof(RelationshipPreference.ReminderEnabled),
                    HumanFlag(preference.ReminderEnabled), HumanFlag(true), source);
                await AuditAsync(ownerId, request.PersonId, nameof(RelationshipPreference.ReminderIntervalDays),
                    HumanDays(preference.ReminderIntervalDays), HumanDays(request.IntervalDays), source);
                await AuditAsync(ownerId, request.PersonId, nameof(RelationshipPreference.ReminderStrict),
                    preference.ReminderStrict ? "strict" : "flexible", request.Strict ? "strict" : "flexible", source);
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
            // Completion is ONLY allowed via logging an actual interaction.
            // This endpoint exists for backward compatibility but no longer
            // marks anything complete: it tells the caller where to go
            // instead. InteractionService.LogAsync is the sole writer of
            // LastCompletedAtUtc.
            var ownerId = OwnerId();
            await RequireOwnedPersonAsync(ownerId, personId);
            var preference = await _preferences.GetAsync(ownerId, personId)
                ?? throw new KeyNotFoundException("No reminder is configured for this person.");
            if (preference.ReminderEnabled)
                throw new InvalidOperationException(
                    "Marking done requires logging the actual interaction first — " +
                    "log it (Person page, Copilot, or Meeting confirm) and this cycle clears itself.");
            var dtoPerson = await _persons.GetPersonById(personId);
            return ToDto(preference, dtoPerson?.Name);
        }

        public async Task<RelationshipPreferenceDto> DisableReminderAsync(Guid personId, PreferenceChangeSource source = PreferenceChangeSource.User)
        {
            var ownerId = OwnerId();
            var person = await RequireOwnedPersonAsync(ownerId, personId);
            var preference = await _preferences.GetAsync(ownerId, personId)
                ?? throw new KeyNotFoundException("No reminder is configured for this person.");
            await AuditAsync(ownerId, personId, nameof(RelationshipPreference.ReminderEnabled),
                HumanFlag(preference.ReminderEnabled), HumanFlag(false), source);
            preference.ReminderEnabled = false;
            preference.SnoozedUntilUtc = null;
            preference.UpdatedAtUtc = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync();
            return ToDto(preference, person.Name);
        }

        public async Task RemoveAsync(Guid personId, PreferenceChangeSource source = PreferenceChangeSource.User)
        {
            var ownerId = OwnerId();
            await RequireOwnedPersonAsync(ownerId, personId);
            var preference = await _preferences.GetAsync(ownerId, personId);
            if (preference == null)
                return;
            // One audit row per previously-set field so the revert is
            // explainable: removing the override returns the relationship to
            // global defaults, then system inference.
            if (preference.DesiredCadenceDays != null)
                await AuditAsync(ownerId, personId, nameof(RelationshipPreference.DesiredCadenceDays),
                    HumanDays(preference.DesiredCadenceDays), HumanDays(null), source);
            if (preference.Importance != 0)
                await AuditAsync(ownerId, personId, nameof(RelationshipPreference.Importance),
                    HumanLevel(preference.Importance), HumanLevel(0), source);
            if (preference.Priority != 0)
                await AuditAsync(ownerId, personId, nameof(RelationshipPreference.Priority),
                    HumanLevel(preference.Priority), HumanLevel(0), source);
            if (preference.KeepInTouchIntentionally)
                await AuditAsync(ownerId, personId, nameof(RelationshipPreference.KeepInTouchIntentionally),
                    HumanFlag(true), HumanFlag(false), source);
            if (preference.ExcludeFromSuggestions)
                await AuditAsync(ownerId, personId, nameof(RelationshipPreference.ExcludeFromSuggestions),
                    HumanFlag(true), HumanFlag(false), source);
            if (preference.ReminderEnabled)
                await AuditAsync(ownerId, personId, nameof(RelationshipPreference.ReminderEnabled),
                    HumanFlag(true), HumanFlag(false), source);
            await _preferences.RemoveAsync(preference);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<string> GetReminderStateAsync(Guid personId)
        {
            var ownerId = OwnerId();
            await RequireOwnedPersonAsync(ownerId, personId);
            var preference = await _preferences.GetAsync(ownerId, personId);
            if (preference == null || !preference.ReminderEnabled)
                return ReminderState.Disabled.ToString();
            var now = DateTime.UtcNow;
            if (preference.SnoozedUntilUtc != null && preference.SnoozedUntilUtc > now)
            {
                // Skipped cycles reuse the snooze field with a skip stamp;
                // a plain snooze has no LastSkipped marker.
                if (preference.LastSkippedAtUtc != null
                    && preference.SnoozedUntilUtc > preference.LastSkippedAtUtc.Value.AddDays(-1))
                    return ReminderState.Skipped.ToString();
                return ReminderState.Snoozed.ToString();
            }
            if (preference.LastCompletedAtUtc != null
                && (preference.SnoozedUntilUtc == null || preference.SnoozedUntilUtc <= now))
            {
                var interval = preference.ReminderIntervalDays ?? 7;
                if ((now - preference.LastCompletedAtUtc.Value).TotalDays < interval)
                    return ReminderState.Completed.ToString();
            }
            var due = await ListDueAsync();
            return due.Any(d => d.PersonId == personId)
                ? ReminderState.Due.ToString()
                : ReminderState.Idle.ToString();
        }

        public async Task<List<PreferenceAuditDto>> GetHistoryAsync(Guid personId)
        {
            var ownerId = OwnerId();
            await RequireOwnedPersonAsync(ownerId, personId);
            if (_audit == null)
                return new List<PreferenceAuditDto>();
            var entries = await _audit.ListForPersonAsync(ownerId, personId);
            return entries.Select(e => new PreferenceAuditDto
            {
                PersonId = e.PersonId,
                Field = e.Field,
                PreviousValue = e.PreviousValue,
                NewValue = e.NewValue,
                Source = e.Source.ToString(),
                ChangedAtUtc = e.ChangedAtUtc
            }).ToList();
        }

        public async Task<GlobalDefaultsDto> GetGlobalDefaultsAsync()
        {
            OwnerId();
            if (_defaults == null)
                return new GlobalDefaultsDto();
            var ownerId = OwnerId();
            var current = await _defaults.GetAsync(ownerId);
            if (current == null)
                return new GlobalDefaultsDto();
            return new GlobalDefaultsDto
            {
                DefaultCadenceDays = current.DefaultCadenceDays,
                DefaultReminderStrict = current.DefaultReminderStrict
            };
        }

        public async Task<GlobalDefaultsDto> SaveGlobalDefaultsAsync(GlobalDefaultsSaveRequest request, PreferenceChangeSource source = PreferenceChangeSource.User)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (request.DefaultCadenceDays != null)
                ValidateInterval(request.DefaultCadenceDays.Value, nameof(request.DefaultCadenceDays));
            var ownerId = OwnerId();
            if (_defaults == null)
                throw new InvalidOperationException("Global defaults are not available.");
            var current = await _defaults.GetAsync(ownerId);
            var previous = current?.DefaultCadenceDays;
            await _defaults.UpsertAsync(new GlobalPreferenceDefaults
            {
                ApplicationUserId = ownerId,
                DefaultCadenceDays = request.DefaultCadenceDays ?? current?.DefaultCadenceDays,
                DefaultReminderStrict = request.DefaultReminderStrict ?? current?.DefaultReminderStrict ?? false,
                UpdatedAtUtc = DateTime.UtcNow
            });
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Global defaults changed for {OwnerId} from {Previous} by {Source}",
                ownerId, HumanDays(previous), source);
            // Read back through the same repository contract so mocks and the
            // real store behave identically (Upsert mutates the tracked row).
            var refreshed = await _defaults.GetAsync(ownerId);
            return new GlobalDefaultsDto
            {
                DefaultCadenceDays = refreshed?.DefaultCadenceDays,
                DefaultReminderStrict = refreshed?.DefaultReminderStrict ?? false
            };
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
