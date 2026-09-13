using System;

namespace ServiceContracts.DTOs.CopilotDTOs
{
    public class BriefingEventItem
    {
        public Guid PersonId { get; set; }

        public string? PersonName { get; set; }

        public string Title { get; set; } = string.Empty;

        public int InDays { get; set; }

        public string SilenceLine { get; set; } = string.Empty;
    }
}
