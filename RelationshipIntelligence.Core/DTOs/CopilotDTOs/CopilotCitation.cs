using System;

namespace ServiceContracts.DTOs.CopilotDTOs
{
    public class CopilotCitation
    {
        public string Kind { get; set; } = string.Empty;

        public Guid? Id { get; set; }

        public string Label { get; set; } = string.Empty;
    }
}
