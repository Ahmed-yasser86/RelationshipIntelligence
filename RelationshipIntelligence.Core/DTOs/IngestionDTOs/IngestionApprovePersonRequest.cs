using System;

namespace ServiceContracts.DTOs.IngestionDTOs
{
    public class IngestionApprovePersonRequest
    {
        public Guid BatchId { get; set; }

        public Guid? PersonId { get; set; }

        public string? PersonName { get; set; }

        public bool IsNew { get; set; }
    }
}
