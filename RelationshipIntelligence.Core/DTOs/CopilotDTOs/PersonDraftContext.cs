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
    }
}
