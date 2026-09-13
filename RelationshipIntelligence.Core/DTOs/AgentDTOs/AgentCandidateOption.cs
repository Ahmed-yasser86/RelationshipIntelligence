using System;

namespace ServiceContracts.DTOs.AgentDTOs
{
    /// <summary>
    /// One disambiguation choice: a distinct label the user picks from popup
    /// buttons, bound to the exact person id so identical names never loop.
    /// Ephemeral session state only — never persisted.
    /// </summary>
    public class AgentCandidateOption
    {
        public Guid PersonId { get; set; }

        public string Label { get; set; } = string.Empty;
    }
}
