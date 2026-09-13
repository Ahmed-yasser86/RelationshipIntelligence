using System;
using System.Collections.Generic;

namespace ServiceContracts.DTOs.MeetingDTOs
{
    public class MeetingCreateRequest
    {
        public string? Title { get; set; }

        public DateTime OccurredAtUtc { get; set; }

        public string? Description { get; set; }

        public string? Agenda { get; set; }

        public string? UserInstructions { get; set; }

        public string? RawTranscript { get; set; }

        public string? RawNotes { get; set; }

        public List<string>? ParticipantNames { get; set; }

        public string? Goal { get; set; }
    }
}
