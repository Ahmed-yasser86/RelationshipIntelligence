using Entities;
using System;

namespace ServiceContracts.DTOs.MeetingDTOs
{
    public class PersonMappingRequest
    {
        public Guid MeetingPersonId { get; set; }

        public Guid? MappedPersonId { get; set; }

        public PersonMatchStatus MatchStatus { get; set; }

        public bool SelectedForLogging { get; set; }
    }
}
