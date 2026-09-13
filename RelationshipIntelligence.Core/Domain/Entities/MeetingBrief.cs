using System;
using System.ComponentModel.DataAnnotations;

namespace Entities
{
    /// <summary>
    /// Durable preparation artifact for a meeting. Generated from prep context,
    /// re-editable by the user, and carried unchanged across the
    /// Preparation → Draft → Confirmed lifecycle. Never an intelligence input.
    /// </summary>
    public class MeetingBrief
    {
        [Key]
        public Guid MeetingBriefId { get; set; }

        [Required]
        public Guid MeetingId { get; set; }

        public Meeting? Meeting { get; set; }

        [StringLength(1000)]
        public string? Goal { get; set; }

        [Required]
        public string BriefJson { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; }

        public DateTime UpdatedAtUtc { get; set; }
    }
}
