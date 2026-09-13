using Entities;
using System;

namespace ServiceContracts.DTOs.OutreachDTOs
{
    public class CommunicationDraftDto
    {
        public Guid CommunicationDraftId { get; set; }

        public Guid PersonId { get; set; }

        public string? PersonName { get; set; }

        public DraftKind Kind { get; set; }

        public OutreachChannel Channel { get; set; }

        public string? Subject { get; set; }

        public string Body { get; set; } = string.Empty;

        public string? ContextUsed { get; set; }

        public bool LimitedContext { get; set; }

        public bool IsAiGenerated { get; set; }

        public DraftStatus Status { get; set; }
    }
}
