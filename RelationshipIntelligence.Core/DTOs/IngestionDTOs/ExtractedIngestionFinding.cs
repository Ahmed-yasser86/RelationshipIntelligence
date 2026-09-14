namespace ServiceContracts.DTOs.IngestionDTOs
{
    public class ExtractedIngestionFinding
    {
        public string Subject { get; set; } = string.Empty;

        public string? Object { get; set; }

        public string? ObjectOrg { get; set; }

        public string? Relation { get; set; }

        public string? TargetField { get; set; }

        public string? MemoryKind { get; set; }

        public string? EventKind { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Detail { get; set; }

        public string? Excerpt { get; set; }

        public string Confidence { get; set; } = "Medium";

        public string? UncertaintyReason { get; set; }
    }
}
