using System;
using System.ComponentModel.DataAnnotations;

namespace Entities
{
    public class RelationshipStateSnapshot
    {
        [Key]
        public Guid RelationshipStateSnapshotId { get; set; }

        [Required]
        public Guid ApplicationUserId { get; set; }

        [Required]
        public Guid PersonId { get; set; }

        public DateTime TakenAtUtc { get; set; }

        public double TieStrength { get; set; }

        public double UrgencyScore { get; set; }

        public string Band { get; set; } = string.Empty;
    }
}
