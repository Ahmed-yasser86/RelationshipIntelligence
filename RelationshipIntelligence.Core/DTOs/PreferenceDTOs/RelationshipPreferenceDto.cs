using System;

namespace ServiceContracts.DTOs.PreferenceDTOs
{
    public class RelationshipPreferenceDto
    {
        public Guid PersonId { get; set; }

        public string? PersonName { get; set; }

        public int? DesiredCadenceDays { get; set; }

        public int Importance { get; set; }

        public int Priority { get; set; }

        public bool KeepInTouchIntentionally { get; set; }

        public bool ExcludeFromSuggestions { get; set; }

        public bool ReminderEnabled { get; set; }

        public int? ReminderIntervalDays { get; set; }

        public bool ReminderStrict { get; set; }

        public DateTime? SnoozedUntilUtc { get; set; }

        public DateTime? LastCompletedAtUtc { get; set; }
    }
}
