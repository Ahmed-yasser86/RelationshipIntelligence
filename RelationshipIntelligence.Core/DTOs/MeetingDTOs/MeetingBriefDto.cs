using System;

namespace ServiceContracts.DTOs.MeetingDTOs
{
    public class MeetingBriefDto
    {
        public string? Goal { get; set; }

        public string BriefJson { get; set; } = string.Empty;

        public DateTime UpdatedAtUtc { get; set; }
    }
}
