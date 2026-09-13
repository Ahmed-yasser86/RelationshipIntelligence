using System;
using System.Collections.Generic;

namespace ServiceContracts.DTOs.MeetingDTOs
{
    public class MeetingExtractionInput
    {
        public string Title { get; set; } = string.Empty;

        public DateTime OccurredAtUtc { get; set; }

        public string? Description { get; set; }

        public string? UserInstructions { get; set; }

        public string? Transcript { get; set; }

        public string? Notes { get; set; }

        public List<KnownMeetingPerson> KnownPeople { get; set; } = new();

        public List<string> ActiveMemoryTitles { get; set; } = new();
    }
}
