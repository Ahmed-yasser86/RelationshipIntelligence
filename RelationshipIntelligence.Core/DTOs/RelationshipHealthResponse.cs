using System;

namespace ServiceContracts.DTOs
{
    public class RelationshipHealthResponse
    {
        public Guid PersonId { get; set; }
        public string Name { get; set; } = string.Empty;
        public double TieStrength { get; set; }
        public DateTime? LastContactAtUtc { get; set; }
        public double? CadenceReferenceDays { get; set; }
        public double? SilenceQuantile { get; set; }
        public double UrgencyScore { get; set; }
        public string Band { get; set; } = string.Empty;
        public bool IsBridge { get; set; }
        public bool IsImportant { get; set; }
    }
}
