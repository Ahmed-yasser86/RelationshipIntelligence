using System;

namespace ServiceContracts.DTOs.MeetingDTOs
{
    public class KnownMeetingPerson
    {
        public Guid PersonId { get; set; }

        public string Name { get; set; } = string.Empty;
    }
}
