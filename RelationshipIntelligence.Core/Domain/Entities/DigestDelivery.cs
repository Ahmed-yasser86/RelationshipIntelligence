using System;
using System.ComponentModel.DataAnnotations;

namespace Entities
{
    public class DigestDelivery
    {
        [Key]
        public Guid DigestDeliveryId { get; set; }

        [Required]
        public Guid ApplicationUserId { get; set; }

        public DateTime WeekStartUtc { get; set; }

        public string PersonIdsJson { get; set; } = "[]";

        public DateTime CreatedAtUtc { get; set; }
    }
}
