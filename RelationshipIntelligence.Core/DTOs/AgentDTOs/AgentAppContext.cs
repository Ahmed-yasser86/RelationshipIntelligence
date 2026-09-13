using System;

namespace ServiceContracts.DTOs.AgentDTOs
{
    /// <summary>
    /// Where the co-pilot was invoked. Carries identifiers and the current
    /// selection — never bulk data. The agent pulls evidence through tools.
    /// </summary>
    public class AgentAppContext
    {
        public string EntryPoint { get; set; } = "general";

        public Guid? PersonId { get; set; }

        public Guid? MeetingId { get; set; }

        public Guid? BatchId { get; set; }

        public Guid[] SelectedPersonIds { get; set; } = Array.Empty<Guid>();
    }
}
