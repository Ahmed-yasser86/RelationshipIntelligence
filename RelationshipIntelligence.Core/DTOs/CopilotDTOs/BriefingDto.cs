using System;
using System.Collections.Generic;

namespace ServiceContracts.DTOs.CopilotDTOs
{
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
