using System;
using System.ComponentModel.DataAnnotations;

namespace Entities
{
    public class RelationshipEvent
    {
        [Key]
        public Guid EventId { get; set; }

        [Required]
        public Guid ApplicationUserId { get; set; }

        [Required]
        public Guid PersonId { get; set; }

        public Person? Person { get; set; }

        [Required]
        public RelationshipEventType Type { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public DateOnly OccursOn { get; set; }

        public bool RepeatsYearly { get; set; }

        [Range(1, 3)]
        public int Importance { get; set; } = 2;

        [StringLength(1000)]
        public string? Notes { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime UpdatedAtUtc { get; set; }
    }
}
