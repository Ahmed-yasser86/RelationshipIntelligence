using System;
using System.Collections.Generic;

namespace ServiceContracts.DTOs.CopilotDTOs
{
    public class BriefingAttentionItem
    {
        public Guid PersonId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Reason { get; set; } = string.Empty;

        public string Band { get; set; } = string.Empty;
    }

    public class BriefingEventItem
    {
        public Guid PersonId { get; set; }

        public string? PersonName { get; set; }

        public string Title { get; set; } = string.Empty;

        public int InDays { get; set; }

        public string SilenceLine { get; set; } = string.Empty;
    }

    public class BriefingChangeItem
    {
        public Guid PersonId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Direction { get; set; } = string.Empty;

        public string Detail { get; set; } = string.Empty;
    }

    public class BriefingDto
    {
        public DateTime GeneratedAtUtc { get; set; }

        public string Summary { get; set; } = string.Empty;

        public List<BriefingAttentionItem> AttentionNow { get; set; } = new();

        public List<BriefingEventItem> UpcomingEvents { get; set; } = new();

        public List<string> FollowUpsDue { get; set; } = new();

        public List<BriefingChangeItem> Changes { get; set; } = new();

        public List<string> SuggestedActions { get; set; } = new();
    }
}
