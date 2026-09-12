using System;
using System.Collections.Generic;

namespace ServiceContracts.DTOs.CopilotDTOs
{
    public class PlanActionDto
    {
        public string Action { get; set; } = string.Empty;

        public Guid? PersonId { get; set; }

        public string? PersonName { get; set; }

        public string WhyNow { get; set; } = string.Empty;

        public string Outcome { get; set; } = string.Empty;
    }

    public class PlanSuggestionDto
    {
        public Guid PersonId { get; set; }

        public string Intent { get; set; } = string.Empty;

        public List<PlanActionDto> SuggestedActions { get; set; } = new();

        public string TimingNotes { get; set; } = string.Empty;

        public bool LimitedContext { get; set; }
    }
}
