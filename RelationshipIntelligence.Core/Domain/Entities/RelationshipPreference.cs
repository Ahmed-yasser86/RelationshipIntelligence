using System;
using System.ComponentModel.DataAnnotations;

namespace Entities
{
    /// <summary>
    /// User-controlled relationship parameters in human terms: intended
    /// contact cadence, importance, suggestion participation, and a reminder
    /// schedule with snooze/skip/complete state. One row per person.
    /// Intention only — it never fabricates interaction history.
    /// </summary>
    public class RelationshipPreference
    {
        [Key]
        public Guid RelationshipPreferenceId { get; set; }

        [Required]
        public Guid ApplicationUserId { get; set; }

        [Required]
        public Guid PersonId { get; set; }

        public Person? Person { get; set; }

        /// <summary>How often the user wants to stay in touch, in days.</summary>
        public int? DesiredCadenceDays { get; set; }

        /// <summary>0 normal, 1 high importance.</summary>
        public int Importance { get; set; }

        /// <summary>0 normal, 1 high priority. Orders attention ties only; never changes scores.</summary>
        public int Priority { get; set; }

        /// <summary>True when the user wants to keep in touch intentionally (special contact).</summary>
        public bool KeepInTouchIntentionally { get; set; }

        /// <summary>True excludes the person from proactive suggestions.</summary>
        public bool ExcludeFromSuggestions { get; set; }

        public bool ReminderEnabled { get; set; }

        /// <summary>Reminder interval in days.</summary>
        public int? ReminderIntervalDays { get; set; }

        /// <summary>Strict reminders surface exactly on schedule; flexible ones tolerate delay.</summary>
        public bool ReminderStrict { get; set; }

        public DateTime? SnoozedUntilUtc { get; set; }

        public DateTime? LastCompletedAtUtc { get; set; }

        public DateTime? LastSkippedAtUtc { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime UpdatedAtUtc { get; set; }
    }
}
