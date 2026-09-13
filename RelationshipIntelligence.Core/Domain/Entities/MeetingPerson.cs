using System;
using System.ComponentModel.DataAnnotations;

namespace Entities
{
    public class MeetingPerson
    {
        [Key]
        public Guid MeetingPersonId { get; set; }

        [Required]
        public Guid MeetingId { get; set; }

        public Meeting? Meeting { get; set; }

        [Required]
        [StringLength(200)]
        public string DetectedName { get; set; } = string.Empty;

        public Guid? MappedPersonId { get; set; }

        public Person? MappedPerson { get; set; }

        [Required]
        public PersonMatchStatus MatchStatus { get; set; } = PersonMatchStatus.Unmapped;

        public bool SelectedForLogging { get; set; }
    }
}
