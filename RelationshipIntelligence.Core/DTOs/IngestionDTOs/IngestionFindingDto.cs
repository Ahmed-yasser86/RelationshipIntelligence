using System;
using System.Collections.Generic;

namespace ServiceContracts.DTOs.IngestionDTOs
{
    public class IngestionFindingDto
    {
        public Guid IngestionFindingId { get; set; }

        public Guid IngestionBatchId { get; set; }

        public Guid? SubjectPersonId { get; set; }

        public string? SubjectPersonName { get; set; }

        public bool SubjectIsNew { get; set; }

        public string? SubjectName { get; set; }

        public string? ObjectName { get; set; }

        public string? ObjectOrg { get; set; }

        public string? RelationKind { get; set; }

        public string? TargetField { get; set; }

        public string? MemoryKind { get; set; }

        public string? EventKind { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Detail { get; set; }

        public string? SourceExcerpt { get; set; }

        public string Confidence { get; set; } = string.Empty;

        public string? UncertaintyReason { get; set; }

        public string ConflictType { get; set; } = string.Empty;

        public string? ExistingValue { get; set; }

        public string ProposalAction { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public List<ResolutionCandidateDto> Candidates { get; set; } = new();
    }
}
