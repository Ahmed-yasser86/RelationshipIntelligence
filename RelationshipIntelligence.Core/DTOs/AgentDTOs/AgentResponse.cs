using ServiceContracts.DTOs.CopilotDTOs;
using System;
using System.Collections.Generic;

namespace ServiceContracts.DTOs.AgentDTOs
{
    public class AgentResponse
    {
        public Guid SessionId { get; set; }

        public string Text { get; set; } = string.Empty;

        public List<CopilotCitation> Citations { get; set; } = new();

        public List<AgentEvidence> Evidence { get; set; } = new();

        public List<AgentAction> Actions { get; set; } = new();

        public AgentWorkingState? WorkingState { get; set; }

        public AgentClarification? NeedsInput { get; set; }

        public bool LimitedContext { get; set; }
    }
}
