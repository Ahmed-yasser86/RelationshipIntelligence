using System;
using System.Collections.Generic;

namespace ServiceContracts.DTOs.OutreachDTOs
{
    public class BuildBatchFromPersonsRequest
    {
        public List<Guid> PersonIds { get; set; } = new();

        public string Intent { get; set; } = "Follow up";
    }
}
