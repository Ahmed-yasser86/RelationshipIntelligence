using ServiceContracts.DTOs.EventDTOs;
using System;
using System.Collections.Generic;

namespace ServiceContracts.DTOs
{
    public class RelationshipHealthResponse
    {
        public Guid PersonId { get; set; }
        public string Name { get; set; } = string.Empty;
        public double TieStrength { get; set; }
        public DateTime? LastContactAtUtc { get; set; }
        public int InteractionCount { get; set; }
        public string EvidenceStatus { get; set; } = string.Empty;
        public double? CadenceReferenceDays { get; set; }
        public double? SilenceQuantile { get; set; }
        public double UrgencyScore { get; set; }
        public string Band { get; set; } = string.Empty;
        public bool IsBridge { get; set; }
        public bool IsImportant { get; set; }

        /// <summary>
        /// Upcoming event occurrences (next 21 days) for this person.
        /// Enrichment only — never changes the score or band.
        /// </summary>
        public List<EventOccurrenceDto> UpcomingEvents { get; set; } = new();

        /// <summary>
        /// True when an event is near AND the relationship is already drifting.
        /// A contextual flag, not a score.
        /// </summary>
        public bool HasEventSignal { get; set; }
    }
}
