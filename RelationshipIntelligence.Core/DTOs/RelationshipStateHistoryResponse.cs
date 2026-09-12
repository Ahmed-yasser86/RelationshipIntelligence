using System;
using System.Collections.Generic;

namespace ServiceContracts.DTOs
{
    public class RelationshipStateHistoryPoint
    {
        public DateTime TakenAtUtc { get; set; }
        public double TieStrength { get; set; }
        public double UrgencyScore { get; set; }
        public string Band { get; set; } = string.Empty;
    }

    public class RelationshipStateHistoryResponse
    {
        public Guid PersonId { get; set; }
        public List<RelationshipStateHistoryPoint> Points { get; set; } = new();
    }
}
