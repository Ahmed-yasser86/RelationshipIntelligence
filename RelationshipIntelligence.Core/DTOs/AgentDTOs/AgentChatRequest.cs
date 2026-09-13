using ServiceContracts.DTOs.CopilotDTOs;
using System;
using System.Collections.Generic;

namespace ServiceContracts.DTOs.AgentDTOs
{
    public class AgentChatRequest
    {
        public Guid? SessionId { get; set; }

        public string Message { get; set; } = string.Empty;

        public AgentAppContext AppContext { get; set; } = new();

        public List<ChatTurnDto> History { get; set; } = new();
    }
}
