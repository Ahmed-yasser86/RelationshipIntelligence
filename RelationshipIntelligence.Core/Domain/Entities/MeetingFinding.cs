using System;
using System.ComponentModel.DataAnnotations;

namespace Entities
{
    public class MeetingFinding
    {
        [Key]
        public Guid MeetingFindingId { get; set; }

        [Required]
        public Guid MeetingId { get; set; }

        public Meeting? Meeting { get; set; }

        public Guid? MappedPersonId { get; set; }

        public Person? MappedPerson { get; set; }

        [Required]
        public FindingKind Kind { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Detail { get; set; }

        [Required]
        public FindingStatus Status { get; set; } = FindingStatus.Suggested;

        [StringLength(500)]
        public string? SourceExcerpt { get; set; }

        [StringLength(500)]
        public string? ResolutionNote { get; set; }

        public Guid? AcceptedAsEntryId { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }
}
