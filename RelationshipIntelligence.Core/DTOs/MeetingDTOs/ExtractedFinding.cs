using Entities;

namespace ServiceContracts.DTOs.MeetingDTOs
{
    public class ExtractedFinding
    {
        public FindingKind Kind { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Detail { get; set; }

        public string? PersonName { get; set; }

        public string? SourceExcerpt { get; set; }
    }
}
