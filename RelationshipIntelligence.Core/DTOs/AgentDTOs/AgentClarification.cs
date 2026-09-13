using System.Collections.Generic;

namespace ServiceContracts.DTOs.AgentDTOs
{
    public class AgentClarification
    {
        public string Prompt { get; set; } = string.Empty;

        public List<string> Options { get; set; } = new();
    }
}
