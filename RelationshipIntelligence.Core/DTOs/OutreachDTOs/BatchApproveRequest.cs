using System;
using System.Collections.Generic;

namespace ServiceContracts.DTOs.OutreachDTOs
{
    public class BatchApproveRequest
    {
        public List<Guid>? DraftIds { get; set; }
    }
}
