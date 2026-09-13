using System;

namespace ServiceContracts.DTOs.MeetingDTOs
{
    public class MeetingTranscriptRequest
    {
        public Guid MeetingId { get; set; }

        public string? RawTranscript { get; set; }

        public string? RawNotes { get; set; }

        public string? UserInstructions { get; set; }
    }
}
