using System;
using System.ComponentModel.DataAnnotations;

namespace Entities
{
    public class RelationshipMemoryEntry
    {
        [Key]
        public Guid MemoryEntryId { get; set; }

        [Required]
        public Guid ApplicationUserId { get; set; }

        [Required]
        public Guid PersonId { get; set; }

        public Person? Person { get; set; }

        [Required]
        public RelationshipMemoryKind Kind { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Detail { get; set; }

        [Required]
        public MemoryEntryStatus Status { get; set; } = MemoryEntryStatus.Active;

        [Required]
        public MemoryProvenance Provenance { get; set; } = MemoryProvenance.User;

        public Guid? SourceMeetingId { get; set; }

        public Guid? SourceFindingId { get; set; }

        [StringLength(500)]
        public string? SourceExcerpt { get; set; }

        public bool SourceMeetingDeleted { get; set; }

        public Guid? CorrectedFromFindingId { get; set; }

        public DateTime? CorrectedAtUtc { get; set; }

        [StringLength(500)]
        public string? CorrectionNote { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime UpdatedAtUtc { get; set; }
    }
}
