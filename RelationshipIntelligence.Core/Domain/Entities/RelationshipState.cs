using System;
using System.ComponentModel.DataAnnotations;

namespace Entities
{
    public class RelationshipState
    {
        [Key]
        public Guid RelationshipStateId { get; set; }

        [Required]
        public Guid ApplicationUserId { get; set; }

        [Required]
        public Guid PersonId { get; set; }

        public double TieStrength { get; set; }

        public DateTime? LastContactAtUtc { get; set; }

        public double? CadenceReferenceDays { get; set; }

        public double? SilenceQuantile { get; set; }

        public bool IsBridge { get; set; }

        public double UrgencyScore { get; set; }

        public DateTime UpdatedAtUtc { get; set; }
    }
}
