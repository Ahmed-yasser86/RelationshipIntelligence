using System;

namespace ServiceContracts.DTOs
{
    public class NetworkNodeResponse
    {
        public Guid PersonId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Degree { get; set; }
        public bool IsBridge { get; set; }
        public bool IsIsolated { get; set; }
        public double UrgencyScore { get; set; }
        public string EvidenceStatus { get; set; } = string.Empty;
    }
}
