using System.Collections.Generic;

namespace ServiceContracts.DTOs.IngestionDTOs
{
    public class IngestionApplyResult
    {
        public int AppliedCount { get; set; }

        public int SkippedCount { get; set; }

        public List<string> Applied { get; set; } = new();

        public List<string> Skipped { get; set; } = new();
    }
}
