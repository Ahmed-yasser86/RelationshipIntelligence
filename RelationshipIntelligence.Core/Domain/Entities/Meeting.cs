using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Entities
{
    public class Meeting
    {
        [Key]
        public Guid MeetingId { get; set; }

        [Required]
        public Guid ApplicationUserId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Planned meeting time. Set at preparation/draft creation.
        /// </summary>
        [Required]
        public DateTime OccurredAtUtc { get; set; }

        /// <summary>
        /// Actual occurrence time, confirmed when the meeting is logged.
        /// Null until confirmed; interactions use this (falling back to planned).
        /// Planned and actual are never conflated.
        /// </summary>
        public DateTime? ActualOccurredAtUtc { get; set; }

        [StringLength(2000)]
        public string? Description { get; set; }

        [StringLength(2000)]
        public string? Agenda { get; set; }

        [StringLength(1000)]
        public string? UserInstructions { get; set; }

        public string? RawTranscript { get; set; }

        public string? RawNotes { get; set; }

        /// <summary>
        /// Structured summary produced by extraction. Stored finding detail —
        /// reviewable, never intelligence input on its own.
        /// </summary>
        [StringLength(4000)]
        public string? ProcessedSummary { get; set; }

        [Required]
        public MeetingStatus Status { get; set; } = MeetingStatus.Draft;

        public DateTime CreatedAtUtc { get; set; }

        public DateTime UpdatedAtUtc { get; set; }

        public ICollection<MeetingPerson> People { get; set; } = new List<MeetingPerson>();

        public ICollection<MeetingFinding> Findings { get; set; } = new List<MeetingFinding>();

        public MeetingBrief? Brief { get; set; }
    }
}
