using System;
using System.Collections.Generic;

namespace ServiceContracts.DTOs.AgentDTOs
{
    public class AgentWorkingState
    {
        public string? CurrentTask { get; set; }

        public List<string> SelectedPeople { get; set; } = new();

        public Guid? BatchId { get; set; }

        public Guid? MeetingId { get; set; }

        public string? Channel { get; set; }

        public string? Intent { get; set; }

        public List<string> PendingApprovals { get; set; } = new();
    }
}
