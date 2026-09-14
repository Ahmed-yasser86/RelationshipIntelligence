using System.Collections.Generic;

namespace ServiceContracts.DTOs.IngestionDTOs
{
    public class IngestionExtraction
    {
        public string Summary { get; set; } = string.Empty;

        public bool NoOp { get; set; }

        public string? NoOpReason { get; set; }

        public List<ExtractedIngestionEntity> Entities { get; set; } = new();

        public List<ExtractedIngestionFinding> Findings { get; set; } = new();
    }
}
