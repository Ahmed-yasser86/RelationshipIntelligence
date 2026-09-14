using System;

namespace ServiceContracts.DTOs.PreferenceDTOs
{
    /// <summary>
    /// A due user-configured reminder. Intention, not a relationship verdict:
    /// it says the user asked to be reminded, nothing about urgency.
    /// </summary>
    public class ReminderDueDto
    {
        public Guid PersonId { get; set; }

        public string? PersonName { get; set; }

        public int IntervalDays { get; set; }

        public bool Strict { get; set; }

        public int? SilenceDays { get; set; }

        public string SourceLabel { get; set; } = "You configured this reminder.";
    }
}
