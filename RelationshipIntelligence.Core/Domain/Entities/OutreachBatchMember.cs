using System;
using System.ComponentModel.DataAnnotations;

namespace Entities
{
    public class OutreachBatchMember
    {
        [Key]
        public Guid OutreachBatchMemberId { get; set; }

        [Required]
        public Guid OutreachBatchId { get; set; }

        public OutreachBatch? Batch { get; set; }

        [Required]
        public Guid PersonId { get; set; }

        public Person? Person { get; set; }

        [Required]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;

        public OutreachChannel? ChannelOverride { get; set; }

        [StringLength(500)]
        public string? IntentOverride { get; set; }

        [StringLength(1000)]
        public string? CustomInstruction { get; set; }

        public bool Excluded { get; set; }

        public bool SkipFutureSuggestions { get; set; }

        public DateTime AddedAtUtc { get; set; }
    }
}
