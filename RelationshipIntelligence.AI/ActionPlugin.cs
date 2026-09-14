using Entities;
using Microsoft.SemanticKernel;
using ServiceContracts;
using ServiceContracts.DTOs;
using System;
using System.ComponentModel;
using System.Text.Json;
using System.Threading.Tasks;

namespace RelationshipIntelligence.AI
{
    /// <summary>
    /// Mutating co-pilot capabilities. Every tool takes an explicit confirmed
    /// flag: the agent may only set it after the user confirmed the action in
    /// conversation, or when the triggering message is itself an explicit
    /// instruction ("log a call with Salma"). All writes flow through the
    /// existing owner-scoped services — the agent has no other access to
    /// persistence.
    /// </summary>
    public sealed class ActionPlugin
    {
        private static readonly JsonSerializerOptions Json = new()
        {
            WriteIndented = false,
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        };

        private readonly IEventService _events;
        private readonly IRelationshipMemoryService _memory;
        private readonly IInteractionService _interactions;
        private readonly IMeetingService _meetings;
        private readonly IRelationshipPreferenceService _preferences;

        public ActionPlugin(
            IEventService events,
            IRelationshipMemoryService memory,
            IInteractionService interactions,
            IMeetingService meetings,
            IRelationshipPreferenceService preferences)
        {
            _events = events;
            _memory = memory;
            _interactions = interactions;
            _meetings = meetings;
            _preferences = preferences;
        }

        private static string NeedConfirmation(string what) =>
            JsonSerializer.Serialize(new { needsConfirmation = true, prompt = $"Confirm to proceed: {what}" }, Json);

        [KernelFunction, Description("Record an event (birthday, milestone, custom) for a person. Requires confirmation.")]
        public async Task<string> CreateEventAsync(
            [Description("The person's id (Guid).")] string personId,
            [Description("Event type: Birthday, Anniversary, Holiday, JobChange, ProfessionalMilestone, ProjectMilestone, PersonalMilestone, Custom.")] string type,
            [Description("Event title.")] string title,
            [Description("Date yyyy-MM-dd.")] string occursOn,
            [Description("Whether it repeats yearly.")] bool repeatsYearly = false,
            [Description("Set true only after the user confirmed, or when the request itself explicitly instructs this.")] bool confirmed = false)
        {
            if (!confirmed)
                return NeedConfirmation($"record '{title}' ({type})");
            if (!Guid.TryParse(personId, out var id))
                return JsonSerializer.Serialize(new { error = "Invalid person id." }, Json);
            if (!Enum.TryParse<RelationshipEventType>(type, true, out var eventType))
                return JsonSerializer.Serialize(new { error = "Unknown event type." }, Json);
            if (!DateOnly.TryParse(occursOn, out var date))
                return JsonSerializer.Serialize(new { error = "Date must be yyyy-MM-dd." }, Json);
            try
            {
                var created = await _events.CreateAsync(new ServiceContracts.DTOs.EventDTOs.EventCreateRequest
                {
                    PersonId = id,
                    Type = eventType,
                    Title = title,
                    OccursOn = date,
                    RepeatsYearly = repeatsYearly
                });
                return JsonSerializer.Serialize(new { created.EventId, created.Title }, Json);
            }
            catch (Exception ex) when (ex is KeyNotFoundException || ex is ArgumentException)
            {
                return JsonSerializer.Serialize(new { error = ex.Message }, Json);
            }
        }

        [KernelFunction, Description("Edit an existing relationship memory entry. User edits are authoritative. Requires confirmation.")]
        public async Task<string> UpdateRelationshipContextAsync(
            [Description("The memory entry id (Guid).")] string entryId,
            [Description("Corrected title.")] string title,
            [Description("Corrected detail, or empty.")] string detail = "",
            [Description("Set true only after the user confirmed, or when the request itself explicitly instructs this.")] bool confirmed = false)
        {
            if (!confirmed)
                return NeedConfirmation("update that memory entry");
            if (!Guid.TryParse(entryId, out var id))
                return JsonSerializer.Serialize(new { error = "Invalid entry id." }, Json);
            try
            {
                var updated = await _memory.UpdateAsync(new ServiceContracts.DTOs.MemoryDTOs.MemoryEntryUpdateRequest
                {
                    MemoryEntryId = id,
                    Title = title,
                    Detail = string.IsNullOrWhiteSpace(detail) ? null : detail
                });
                return JsonSerializer.Serialize(new { updated.MemoryEntryId, updated.Title }, Json);
            }
            catch (Exception ex) when (ex is KeyNotFoundException || ex is ArgumentException)
            {
                return JsonSerializer.Serialize(new { error = ex.Message }, Json);
            }
        }

        [KernelFunction, Description("Create a relationship goal for a person. Requires confirmation.")]
        public async Task<string> CreateGoalAsync(
            [Description("The person's id (Guid).")] string personId,
            [Description("Goal title.")] string title,
            [Description("Goal detail, or empty.")] string detail = "",
            [Description("Set true only after the user confirmed, or when the request itself explicitly instructs this.")] bool confirmed = false)
        {
            if (!confirmed)
                return NeedConfirmation($"create the goal '{title}'");
            if (!Guid.TryParse(personId, out var id))
                return JsonSerializer.Serialize(new { error = "Invalid person id." }, Json);
            try
            {
                var created = await _memory.CreateAsync(new ServiceContracts.DTOs.MemoryDTOs.MemoryEntryCreateRequest
                {
                    PersonId = id,
                    Kind = RelationshipMemoryKind.Goal,
                    Title = title,
                    Detail = string.IsNullOrWhiteSpace(detail) ? null : detail
                });
                return JsonSerializer.Serialize(new { created.MemoryEntryId, created.Title }, Json);
            }
            catch (Exception ex) when (ex is KeyNotFoundException || ex is ArgumentException)
            {
                return JsonSerializer.Serialize(new { error = ex.Message }, Json);
            }
        }

        [KernelFunction, Description("Log an interaction for a person. The user instruction to log IS the confirmation for straightforward logs; set confirmed accordingly.")]
        public async Task<string> LogInteractionAsync(
            [Description("The person's id (Guid).")] string personId,
            [Description("Type: Call, Email, Meeting, Message.")] string type,
            [Description("Short title.")] string title,
            [Description("Set true when the user asked to log this (their message is the instruction).")] bool confirmed = false)
        {
            if (!confirmed)
                return NeedConfirmation($"log '{title}' ({type})");
            if (!Guid.TryParse(personId, out var id))
                return JsonSerializer.Serialize(new { error = "Invalid person id." }, Json);
            if (!Enum.TryParse<ContactsManger.Core.Domain.Entities.EEnums.EnInteractionType>(type, true, out var interactionType))
                return JsonSerializer.Serialize(new { error = "Type must be Call, Email, Meeting, or Message." }, Json);
            try
            {
                var logged = await _interactions.LogAsync(new InteractionAddRequest
                {
                    PersonId = id,
                    TimeOfInteraction = DateTime.UtcNow,
                    InteractionType = interactionType,
                    InteractionTitle = title
                });
                return JsonSerializer.Serialize(new { logged.InteractionId, logged.InteractionTitle }, Json);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is System.ComponentModel.DataAnnotations.ValidationException || ex is UnauthorizedAccessException)
            {
                return JsonSerializer.Serialize(new { error = ex.Message }, Json);
            }
        }

        [KernelFunction, Description("Create a meeting draft with participants and optional notes. Reviewable draft only; processing and confirmation are separate user-approved steps.")]
        public async Task<string> CreateMeetingAsync(
            [Description("Meeting title.")] string title,
            [Description("Comma-separated participant names.")] string participants,
            [Description("Transcript or notes, or empty.")] string notes = "",
            [Description("Set true only after the user confirmed, or when the request itself explicitly instructs this.")] bool confirmed = false)
        {
            if (!confirmed)
                return NeedConfirmation($"create the meeting '{title}'");
            if (string.IsNullOrWhiteSpace(title))
                return JsonSerializer.Serialize(new { error = "Title is required." }, Json);
            try
            {
                var created = await _meetings.CreateDraftAsync(new ServiceContracts.DTOs.MeetingDTOs.MeetingCreateRequest
                {
                    Title = title.Trim(),
                    OccurredAtUtc = DateTime.UtcNow,
                    RawNotes = string.IsNullOrWhiteSpace(notes) ? null : notes,
                    ParticipantNames = new System.Collections.Generic.List<string>(
                        (participants ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                });
                return JsonSerializer.Serialize(new { created.MeetingId, created.Title }, Json);
            }
            catch (ArgumentException ex)
            {
                return JsonSerializer.Serialize(new { error = ex.Message }, Json);
            }
        }

        [KernelFunction, Description("Accept or reject one extracted meeting finding. Accepting commitment/fact findings creates reviewable memory. Requires confirmation.")]
        public async Task<string> ConfirmMeetingFindingAsync(
            [Description("The finding id (Guid).")] string findingId,
            [Description("accept or reject.")] string decision,
            [Description("Set true only after the user confirmed.")] bool confirmed = false)
        {
            if (!confirmed)
                return NeedConfirmation($"{decision} that finding");
            if (!Guid.TryParse(findingId, out var id))
                return JsonSerializer.Serialize(new { error = "Invalid finding id." }, Json);
            var accept = string.Equals(decision, "accept", StringComparison.OrdinalIgnoreCase);
            if (!accept && !string.Equals(decision, "reject", StringComparison.OrdinalIgnoreCase))
                return JsonSerializer.Serialize(new { error = "Decision must be accept or reject." }, Json);
            try
            {
                var reviewed = await _meetings.ReviewFindingAsync(new ServiceContracts.DTOs.MeetingDTOs.FindingReviewRequest
                {
                    MeetingFindingId = id,
                    Status = accept ? Entities.FindingStatus.Accepted : Entities.FindingStatus.Rejected
                });
                return JsonSerializer.Serialize(new { reviewed.MeetingId, findingId, decision }, Json);
            }
            catch (Exception ex) when (ex is KeyNotFoundException || ex is ArgumentException || ex is InvalidOperationException)
            {
                return JsonSerializer.Serialize(new { error = ex.Message }, Json);
            }
        }

        [KernelFunction, Description("Save relationship preferences for a person: desired contact cadence in days, importance (0/1), priority (0/1), intentional contact (true/false), exclusion from proactive suggestions (true/false). Human terms only — never scores or weights. Requires confirmation.")]
        public async Task<string> SavePreferenceAsync(
            [Description("The person's id (Guid).")] string personId,
            [Description("Desired contact cadence in days (1-365), or -1 to leave unchanged.")] int cadenceDays = -1,
            [Description("Importance 0 or 1, or -1 to leave unchanged.")] int importance = -1,
            [Description("Priority 0 or 1, or -1 to leave unchanged.")] int priority = -1,
            [Description("Keep in touch intentionally (special contact).")] bool intentional = false,
            [Description("Set true to change the intentional-contact flag.")] bool changeIntentional = false,
            [Description("Exclude from proactive suggestions.")] bool excludeFromSuggestions = false,
            [Description("Set true to change the suggestion-inclusion flag.")] bool changeExclusion = false,
            [Description("Set true only after the user confirmed.")] bool confirmed = false)
        {
            if (!confirmed)
                return NeedConfirmation("save those relationship preferences");
            if (!Guid.TryParse(personId, out var id))
                return JsonSerializer.Serialize(new { error = "Invalid person id." }, Json);
            try
            {
                var saved = await _preferences.SaveAsync(new ServiceContracts.DTOs.PreferenceDTOs.PreferenceSaveRequest
                {
                    PersonId = id,
                    DesiredCadenceDays = cadenceDays < 0 ? null : cadenceDays,
                    Importance = importance < 0 ? null : importance,
                    Priority = priority < 0 ? null : priority,
                    KeepInTouchIntentionally = changeIntentional ? intentional : null,
                    ExcludeFromSuggestions = changeExclusion ? excludeFromSuggestions : null
                });
                return JsonSerializer.Serialize(saved, Json);
            }
            catch (Exception ex) when (ex is KeyNotFoundException || ex is ArgumentException)
            {
                return JsonSerializer.Serialize(new { error = ex.Message }, Json);
            }
        }

        [KernelFunction, Description("Set a per-person reminder: remind the user about this person every N days. A reminder is intention, not urgency — it never changes the relationship score. Requires confirmation.")]
        public async Task<string> SetReminderAsync(
            [Description("The person's id (Guid).")] string personId,
            [Description("Reminder interval in days (1-365).")] int intervalDays,
            [Description("Strict reminders surface exactly on schedule.")] bool strict = false,
            [Description("Set true only after the user confirmed.")] bool confirmed = false)
        {
            if (!confirmed)
                return NeedConfirmation($"remind about this person every {intervalDays} days");
            if (!Guid.TryParse(personId, out var id))
                return JsonSerializer.Serialize(new { error = "Invalid person id." }, Json);
            try
            {
                var saved = await _preferences.SetReminderAsync(new ServiceContracts.DTOs.PreferenceDTOs.ReminderSetRequest
                {
                    PersonId = id,
                    IntervalDays = intervalDays,
                    Strict = strict
                });
                return JsonSerializer.Serialize(saved, Json);
            }
            catch (Exception ex) when (ex is KeyNotFoundException || ex is ArgumentException)
            {
                return JsonSerializer.Serialize(new { error = ex.Message }, Json);
            }
        }

        [KernelFunction, Description("Snooze a person's reminder for N days. Viewing or snoozing never records an interaction. Requires confirmation.")]
        public async Task<string> SnoozeReminderAsync(
            [Description("The person's id (Guid).")] string personId,
            [Description("Snooze length in days (1-90).")] int days,
            [Description("Set true only after the user confirmed.")] bool confirmed = false)
        {
            if (!confirmed)
                return NeedConfirmation($"snooze that reminder for {days} days");
            if (!Guid.TryParse(personId, out var id))
                return JsonSerializer.Serialize(new { error = "Invalid person id." }, Json);
            try
            {
                var saved = await _preferences.SnoozeAsync(id, days);
                return JsonSerializer.Serialize(saved, Json);
            }
            catch (Exception ex) when (ex is KeyNotFoundException || ex is ArgumentException)
            {
                return JsonSerializer.Serialize(new { error = ex.Message }, Json);
            }
        }

        [KernelFunction, Description("Disable a person's reminder entirely. Requires confirmation.")]
        public async Task<string> DisableReminderAsync(
            [Description("The person's id (Guid).")] string personId,
            [Description("Set true only after the user confirmed.")] bool confirmed = false)
        {
            if (!confirmed)
                return NeedConfirmation("turn off that reminder");
            if (!Guid.TryParse(personId, out var id))
                return JsonSerializer.Serialize(new { error = "Invalid person id." }, Json);
            try
            {
                var saved = await _preferences.DisableReminderAsync(id);
                return JsonSerializer.Serialize(saved, Json);
            }
            catch (KeyNotFoundException ex)
            {
                return JsonSerializer.Serialize(new { error = ex.Message }, Json);
            }
        }

        [KernelFunction, Description("Accept a pending AI memory suggestion, making it user-confirmed. Requires confirmation.")]
        public async Task<string> PersistMemorySuggestionAsync(
            [Description("The memory entry id (Guid).")] string entryId,
            [Description("Set true only after the user confirmed.")] bool confirmed = false)
        {
            if (!confirmed)
                return NeedConfirmation("accept that suggestion");
            if (!Guid.TryParse(entryId, out var id))
                return JsonSerializer.Serialize(new { error = "Invalid entry id." }, Json);
            try
            {
                var accepted = await _memory.AcceptSuggestionAsync(id);
                return JsonSerializer.Serialize(new { accepted.MemoryEntryId, accepted.Title }, Json);
            }
            catch (Exception ex) when (ex is KeyNotFoundException || ex is InvalidOperationException)
            {
                return JsonSerializer.Serialize(new { error = ex.Message }, Json);
            }
        }
    }
}
