using Entities;
using System;

namespace ServiceContracts.DTOs.MeetingDTOs
{
    public class MeetingPersonDto
    {
        public Guid MeetingPersonId { get; set; }

        public string DetectedName { get; set; } = string.Empty;

        public Guid? MappedPersonId { get; set; }

        public string? MappedPersonName { get; set; }

        public PersonMatchStatus MatchStatus { get; set; }

        public bool SelectedForLogging { get; set; }
    }
}
