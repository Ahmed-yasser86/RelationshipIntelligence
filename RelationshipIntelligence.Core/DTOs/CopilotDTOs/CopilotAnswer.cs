using System.Collections.Generic;

namespace ServiceContracts.DTOs.CopilotDTOs
{
    public class CopilotAnswer
    {
        public string Text { get; set; } = string.Empty;

        public List<CopilotCitation> Citations { get; set; } = new();

        public bool LimitedContext { get; set; }
    }
}
