using System;

namespace ServiceContracts.DTOs.MeetingDTOs
{
    public class BriefSaveRequest
    {
        public Guid MeetingId { get; set; }

        public string? Goal { get; set; }

        public string? BriefJson { get; set; }
    }
}
