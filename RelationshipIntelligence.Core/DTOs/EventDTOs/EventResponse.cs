using Entities;
using System;

namespace ServiceContracts.DTOs.EventDTOs
{
    public class EventResponse
    {
        public Guid EventId { get; set; }

        public Guid PersonId { get; set; }

        public string? PersonName { get; set; }

        public RelationshipEventType Type { get; set; }

        public string Title { get; set; } = string.Empty;

        public DateOnly OccursOn { get; set; }

        public bool RepeatsYearly { get; set; }

        public int Importance { get; set; }

        public string? Notes { get; set; }

        public static EventResponse FromEvent(RelationshipEvent relationshipEvent, string? personName = null) => new()
        {
            EventId = relationshipEvent.EventId,
            PersonId = relationshipEvent.PersonId,
            PersonName = personName,
            Type = relationshipEvent.Type,
            Title = relationshipEvent.Title,
            OccursOn = relationshipEvent.OccursOn,
            RepeatsYearly = relationshipEvent.RepeatsYearly,
            Importance = relationshipEvent.Importance,
            Notes = relationshipEvent.Notes
        };
    }
}
