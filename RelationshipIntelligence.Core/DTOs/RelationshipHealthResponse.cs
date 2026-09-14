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
        /// <summary>
        /// Canonical silence, computed server-side via TieDecayModel.SilenceDays
        /// at query time. Clients must display this instead of recomputing from
        /// LastContactAtUtc (client clocks skew by hours and reintroduce off-by-one).
        /// Null when LastContactAtUtc is null.
        /// </summary>
        public int? SilenceDays { get; set; }
        public int InteractionCount { get; set; }
        public string EvidenceStatus { get; set; } = string.Empty;
        public double? CadenceReferenceDays { get; set; }
        /// <summary>
        /// User-configured contact cadence in days ("how often I want to stay
        /// in touch"). Intention, not measurement: the deterministic model
        /// (strength, urgency, rhythm) never reads it. It only constrains
        /// attention surfacing (event-signal threshold) below.
        /// </summary>
        public int? DesiredCadenceDays { get; set; }
        /// <summary>True when the user keeps this relationship intentionally.</summary>
        public bool KeepInTouchIntentionally { get; set; }
        /// <summary>
        /// Human-readable source of the effective cadence shown on this row:
        /// "You asked for every N days", "Your default: every N days", or
        /// "Usual rhythm" (system inference). Answers "why this cadence".
        /// </summary>
        public string CadenceSourceLabel { get; set; } = string.Empty;
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
