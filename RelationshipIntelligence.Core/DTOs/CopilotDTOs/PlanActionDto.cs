using System;

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
}
