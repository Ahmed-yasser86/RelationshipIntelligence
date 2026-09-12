using Entities;
using System;

namespace ServiceContracts.DTOs.EventDTOs
{
    /// <summary>
    /// A derived, read-time occurrence of a <see cref="RelationshipEvent"/>.
    /// Never persisted: computed from OccursOn + RepeatsYearly on every read.
    /// </summary>
    public class EventOccurrenceDto
    {
        public Guid EventId { get; set; }

        public Guid PersonId { get; set; }

        public string? PersonName { get; set; }

        public RelationshipEventType Type { get; set; }

        public string Title { get; set; } = string.Empty;

        public DateOnly OccurrenceDate { get; set; }

        public int InDays { get; set; }

        public int Importance { get; set; }
    }
}
