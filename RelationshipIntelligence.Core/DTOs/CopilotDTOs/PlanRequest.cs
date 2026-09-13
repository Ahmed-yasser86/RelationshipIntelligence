using System;

namespace ServiceContracts.DTOs.CopilotDTOs
{
    public class PlanRequest
    {
        public Guid PersonId { get; set; }

        public Guid? IntentEntryId { get; set; }
    }
}
