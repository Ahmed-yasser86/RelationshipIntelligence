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

        /// <summary>
        /// Optional unified-ingestion annotations. Null when the extractor
        /// does not provide them (e.g. meeting extraction); the ingestion
        /// pipeline treats missing confidence as Medium and missing relations
        /// as none. Never deserialized into required behavior.
        /// </summary>
        public string? Confidence { get; set; }

        public string? UncertaintyReason { get; set; }

        public string? ObjectName { get; set; }

        public string? ObjectOrg { get; set; }

        public string? RelationKind { get; set; }

        public string? TargetField { get; set; }
    }
}
