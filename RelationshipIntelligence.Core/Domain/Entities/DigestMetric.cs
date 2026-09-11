using System;
using System.ComponentModel.DataAnnotations;

namespace Entities
{
    public class DigestMetric
    {
        [Key]
        public Guid DigestMetricId { get; set; }

        [Required]
        public Guid DeliveryId { get; set; }

        [Required]
        public Guid ApplicationUserId { get; set; }

        [Required]
        public Guid PersonId { get; set; }

        public bool Opened { get; set; }

        public bool ActionTaken { get; set; }

        public string? ActionType { get; set; }
    }
}
