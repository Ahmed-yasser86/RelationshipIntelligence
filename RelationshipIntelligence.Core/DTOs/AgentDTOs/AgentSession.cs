using System;
using System.Collections.Generic;

namespace ServiceContracts.DTOs.AgentDTOs
{
    /// <summary>
    /// Temporary working state for one co-pilot conversation. Ephemeral by design:
    /// held server-side with sliding expiry, never persisted to the database, and
    /// never mixed into durable relationship memory.
    /// </summary>
    public class AgentSession
    {
        public Guid SessionId { get; set; }

        public Guid? PersonId { get; set; }

        public List<Guid> SelectedPersonIds { get; set; } = new();

        public Guid? BatchId { get; set; }

        public Guid? MeetingId { get; set; }

        public string? Channel { get; set; }

        public string? Intent { get; set; }

        public string? GlobalInstruction { get; set; }

        public Dictionary<Guid, string> Overrides { get; set; } = new();

        public List<string> PendingApprovals { get; set; } = new();

        public string? CurrentTask { get; set; }

        public DateTime UpdatedAtUtc { get; set; }
    }
}
