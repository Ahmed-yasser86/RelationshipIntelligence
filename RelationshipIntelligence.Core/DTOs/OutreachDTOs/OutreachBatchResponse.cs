using Entities;
using System;
using System.Collections.Generic;

namespace ServiceContracts.DTOs.OutreachDTOs
{
    public class OutreachBatchResponse
    {
        public Guid OutreachBatchId { get; set; }

        public string Intent { get; set; } = string.Empty;

        public OutreachChannel Channel { get; set; }

        public string? GlobalInstruction { get; set; }

        public BatchStatus Status { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public List<OutreachBatchMemberDto> Members { get; set; } = new();

        public List<CommunicationDraftDto> Drafts { get; set; } = new();
    }
}
