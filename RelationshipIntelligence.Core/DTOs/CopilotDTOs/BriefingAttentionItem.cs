using System;

namespace ServiceContracts.DTOs.CopilotDTOs
{
    public class BriefingAttentionItem
    {
        public Guid PersonId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Reason { get; set; } = string.Empty;

        public string Band { get; set; } = string.Empty;
    }
}
