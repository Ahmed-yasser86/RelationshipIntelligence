using System;

namespace ServiceContracts.DTOs.IngestionDTOs
{
    public class ExtractedIngestionEntity
    {
        public string Name { get; set; } = string.Empty;

        public string? Organization { get; set; }

        public string? Role { get; set; }

        public Guid? ExistingPersonId { get; set; }

        public bool IsNew { get; set; }

        public bool Ignore { get; set; }

        public string? MatchEvidence { get; set; }

        public string Confidence { get; set; } = "Medium";
    }
}
