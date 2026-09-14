using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Entities
{
    /// <summary>
    /// One ingestion run: raw source text plus every finding extracted from it.
    /// Sources (person text, conversation, group text, meeting text) converge
    /// here; review and approval always operate on this record. Reprocessing
    /// identical text returns the existing batch (SourceTextHash).
    /// </summary>
    public class IngestionBatch
    {
        [Key]
        public Guid IngestionBatchId { get; set; }

        [Required]
        public Guid ApplicationUserId { get; set; }

        [Required]
        public IngestionSourceType SourceType { get; set; }

        /// <summary>Optional link when the source is meeting content.</summary>
        public Guid? SourceMeetingId { get; set; }

        /// <summary>SHA-256 of normalized source text, for idempotency.</summary>
        [Required]
        [StringLength(64)]
        public string SourceTextHash { get; set; } = string.Empty;

        /// <summary>Raw source preserved for provenance and re-review.</summary>
        [Required]
        public string RawText { get; set; } = string.Empty;

        [Required]
        public IngestionStatus Status { get; set; } = IngestionStatus.Pending;

        /// <summary>True when extraction found nothing reliable to propose.</summary>
        public bool IsNoOp { get; set; }

        [StringLength(500)]
        public string? NoOpReason { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime UpdatedAtUtc { get; set; }

        public List<IngestionFinding> Findings { get; set; } = new();
    }
}
