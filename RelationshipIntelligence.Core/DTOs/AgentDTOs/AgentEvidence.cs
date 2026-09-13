using System;

namespace ServiceContracts.DTOs.AgentDTOs
{
    public class AgentEvidence
    {
        public string Title { get; set; } = string.Empty;

        public string Detail { get; set; } = string.Empty;

        public string Kind { get; set; } = "observed";

        public Guid? RefId { get; set; }

        public string? RefKind { get; set; }
    }
}
