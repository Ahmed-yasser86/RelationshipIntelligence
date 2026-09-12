using Entities;
using System;

namespace ServiceContracts.DTOs.EventDTOs
{
    public class EventUpdateRequest
    {
        public Guid EventId { get; set; }

        public RelationshipEventType Type { get; set; }

        public string? Title { get; set; }

        public DateOnly OccursOn { get; set; }

        public bool RepeatsYearly { get; set; }

        public int Importance { get; set; } = 2;

        public string? Notes { get; set; }
    }
}
