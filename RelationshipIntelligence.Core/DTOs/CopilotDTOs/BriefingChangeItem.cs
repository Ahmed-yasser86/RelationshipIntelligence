using System;

namespace ServiceContracts.DTOs.CopilotDTOs
{
    public class BriefingChangeItem
    {
        public Guid PersonId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Direction { get; set; } = string.Empty;

        public string Detail { get; set; } = string.Empty;
    }
}
