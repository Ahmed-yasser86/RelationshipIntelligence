using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Entities
{
    public class OutreachBatch
    {
        [Key]
        public Guid OutreachBatchId { get; set; }

        [Required]
        public Guid ApplicationUserId { get; set; }

        [Required]
        [StringLength(500)]
        public string Intent { get; set; } = string.Empty;

        [Required]
        public OutreachChannel Channel { get; set; }

        [StringLength(1000)]
        public string? GlobalInstruction { get; set; }

        [Required]
        public BatchStatus Status { get; set; } = BatchStatus.Draft;

        public DateTime CreatedAtUtc { get; set; }

        public DateTime UpdatedAtUtc { get; set; }

        public ICollection<OutreachBatchMember> Members { get; set; } = new List<OutreachBatchMember>();

        public ICollection<CommunicationDraft> Drafts { get; set; } = new List<CommunicationDraft>();
    }
}
