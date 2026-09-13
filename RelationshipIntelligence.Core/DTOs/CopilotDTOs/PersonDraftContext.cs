using System;
using System.Collections.Generic;

namespace ServiceContracts.DTOs.CopilotDTOs
{
    public class PersonDraftContext
    {
        public Guid PersonId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Band { get; set; }

        public double? UrgencyScore { get; set; }

        public string? CadenceLine { get; set; }

        public List<string> RecentInteractions { get; set; } = new();

        public List<string> MemoryHighlights { get; set; } = new();

        public List<string> UpcomingEvents { get; set; } = new();

        public List<string> OpenCommitments { get; set; } = new();

        /// <summary>
        /// User-authored communication profile lines. Style guidance only.
        /// </summary>
        public List<string> CommunicationStyle { get; set; } = new();

        /// <summary>
        /// Actual previous messages the user wrote. Few-shot style examples.
        /// </summary>
        public List<string> MessageExamples { get; set; } = new();

        /// <summary>
        /// Approved learnings from edited drafts. Style guidance only.
        /// </summary>
        public List<string> StyleNotes { get; set; } = new();
    }
}
