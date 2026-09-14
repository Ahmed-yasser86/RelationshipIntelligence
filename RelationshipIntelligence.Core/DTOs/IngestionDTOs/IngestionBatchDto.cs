using Entities;
using System;
using System.Collections.Generic;

namespace ServiceContracts.DTOs.IngestionDTOs
{
    public class IngestionBatchDto
    {
        public Guid IngestionBatchId { get; set; }

        public IngestionSourceType SourceType { get; set; }

        public Guid? SourceMeetingId { get; set; }

        public string Status { get; set; } = string.Empty;

        public bool IsNoOp { get; set; }

        public string? NoOpReason { get; set; }

        public int FindingCount { get; set; }

        public int PendingCount { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public List<IngestionFindingDto> Findings { get; set; } = new();
    }
}
