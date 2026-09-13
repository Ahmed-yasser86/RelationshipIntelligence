using System;
using System.Collections.Generic;

namespace ServiceContracts.DTOs.CopilotDTOs
{
    public class CopilotAskRequest
    {
        public string? Question { get; set; }

        public Guid? PersonId { get; set; }

        public List<ChatTurnDto>? History { get; set; }
    }
}
