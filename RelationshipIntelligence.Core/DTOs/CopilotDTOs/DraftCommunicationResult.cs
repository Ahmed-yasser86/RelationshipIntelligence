using System.Collections.Generic;

namespace ServiceContracts.DTOs.CopilotDTOs
{
    public class DraftCommunicationResult
    {
        public string? Subject { get; set; }

        public string Body { get; set; } = string.Empty;

        public List<string> ContextUsed { get; set; } = new();

        public bool LimitedContext { get; set; }
    }
}
