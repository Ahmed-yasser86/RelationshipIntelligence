using System;

namespace ServiceContracts.DTOs.AgentDTOs
{
    /// <summary>
    /// A concrete interaction log awaiting one-word confirmation. Ephemeral
    /// session state only — never persisted.
    /// </summary>
    public class PendingLogProposal
    {
        public Guid PersonId { get; set; }

        public string Type { get; set; } = "Message";

        public string Title { get; set; } = string.Empty;
    }
}
