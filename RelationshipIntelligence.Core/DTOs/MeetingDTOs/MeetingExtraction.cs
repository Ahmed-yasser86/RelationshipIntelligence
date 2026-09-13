using System.Collections.Generic;

namespace ServiceContracts.DTOs.MeetingDTOs
{
    public class MeetingExtraction
    {
        public string Summary { get; set; } = string.Empty;

        public List<string> Topics { get; set; } = new();

        public List<string> Decisions { get; set; } = new();

        public List<ExtractedFinding> Findings { get; set; } = new();

        public List<string> DetectedPeople { get; set; } = new();
    }
}
