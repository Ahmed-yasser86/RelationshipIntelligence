using System;
using System.ComponentModel.DataAnnotations;

namespace Entities
{
    /// <summary>
    /// Canonical finding: one structured proposal from raw text. Carries its
    /// subject (and optional object for entity-to-entity relations), target
    /// slot, confidence, conflict analysis, and approval state. Only Approved
    /// findings may mutate durable relationship state.
    /// </summary>
    public class IngestionFinding
    {
        [Key]
        public Guid IngestionFindingId { get; set; }

        [Required]
        public Guid IngestionBatchId { get; set; }

        public IngestionBatch? Batch { get; set; }

        [Required]
        public Guid ApplicationUserId { get; set; }

        /// <summary>Resolved subject person, if any.</summary>
        public Guid? SubjectPersonId { get; set; }

        public Person? SubjectPerson { get; set; }

        /// <summary>True when the subject is a new person to create on approval.</summary>
        public bool SubjectIsNew { get; set; }

        [StringLength(100)]
        public string? SubjectName { get; set; }

        /// <summary>Resolved object person for entity-to-entity relations.</summary>
        public Guid? ObjectPersonId { get; set; }

        [StringLength(100)]
        public string? ObjectName { get; set; }

        [StringLength(100)]
        public string? ObjectOrg { get; set; }

        /// <summary>
        /// Relation kind, restricted to the supported set: introduced,
        /// working-with, reports-to, referred-by, colleague-of.
        /// </summary>
        [StringLength(40)]
        public string? RelationKind { get; set; }

        /// <summary>Person field slot (role, organization, location, ...), if any.</summary>
        [StringLength(40)]
        public string? TargetField { get; set; }

        /// <summary>Memory slot for memory findings.</summary>
        public RelationshipMemoryKind? MemoryKind { get; set; }

        /// <summary>Event slot for event findings.</summary>
        public string? EventKind { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Detail { get; set; }

        [StringLength(500)]
        public string? SourceExcerpt { get; set; }

        [Required]
        public FindingConfidence Confidence { get; set; } = FindingConfidence.Medium;

        [StringLength(500)]
        public string? UncertaintyReason { get; set; }

        [Required]
        public FindingConflictType ConflictType { get; set; } = FindingConflictType.None;

        /// <summary>Current stored value when this finding would change it.</summary>
        [StringLength(2000)]
        public string? ExistingValue { get; set; }

        [Required]
        public FindingProposalAction ProposalAction { get; set; } = FindingProposalAction.Add;

        [Required]
        public IngestionFindingStatus Status { get; set; } = IngestionFindingStatus.Pending;

        /// <summary>
        /// Reviewer-supplied email for new-person creation (quick-add requires
        /// it). Never invented: approval stays blocked until present.
        /// </summary>
        [StringLength(100)]
        public string? SubjectEmail { get; set; }

        /// <summary>
        /// Reviewer-confirmed date for event findings. Never parsed from prose.
        /// </summary>
        public DateTime? EventDate { get; set; }

        public Guid? AppliedMemoryEntryId { get; set; }

        public Guid? AppliedPersonId { get; set; }

        public Guid? ReviewedById { get; set; }

        public DateTime? ReviewedAtUtc { get; set; }

        [StringLength(500)]
        public string? ReviewNote { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime UpdatedAtUtc { get; set; }
    }
}
