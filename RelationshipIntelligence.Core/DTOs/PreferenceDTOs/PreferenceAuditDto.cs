using System;

namespace ServiceContracts.DTOs.PreferenceDTOs
{
    public class PreferenceAuditDto
    {
        public Guid PersonId { get; set; }

        public string Field { get; set; } = string.Empty;

        public string? PreviousValue { get; set; }

        public string? NewValue { get; set; }

        public string Source { get; set; } = string.Empty;

        public DateTime ChangedAtUtc { get; set; }
    }
}
