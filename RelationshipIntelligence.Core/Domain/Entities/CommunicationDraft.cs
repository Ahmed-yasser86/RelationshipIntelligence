using System;
using System.ComponentModel.DataAnnotations;

namespace Entities
{
    public class CommunicationDraft
    {
        [Key]
        public Guid CommunicationDraftId { get; set; }

        [Required]
        public Guid ApplicationUserId { get; set; }

        [Required]
        public Guid OutreachBatchId { get; set; }

        public OutreachBatch? Batch { get; set; }

        [Required]
        public Guid PersonId { get; set; }

        public Person? Person { get; set; }

        [Required]
        public DraftKind Kind { get; set; } = DraftKind.Message;

        [Required]
        public OutreachChannel Channel { get; set; }

        [StringLength(200)]
        public string? Subject { get; set; }

        [Required]
        public string Body { get; set; } = string.Empty;

        /// <summary>
        /// The original AI-generated body, stashed on first user edit.
        /// Never overwritten: AI draft → user edit history is preserved so
        /// edits become future personalization signals, not lost data.
        /// </summary>
        public string? OriginalBody { get; set; }

        [StringLength(1000)]
        public string? ContextUsed { get; set; }

        public bool LimitedContext { get; set; }

        public bool IsAiGenerated { get; set; } = true;

        [Required]
        public DraftStatus Status { get; set; } = DraftStatus.Draft;

        public DateTime CreatedAtUtc { get; set; }

        public DateTime UpdatedAtUtc { get; set; }
    }
}
