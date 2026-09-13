using Entities;
using System;

namespace ServiceContracts.DTOs.OutreachDTOs
{
    public class OutreachBatchMemberDto
    {
        public Guid OutreachBatchMemberId { get; set; }

        public Guid PersonId { get; set; }

        public string? PersonName { get; set; }

        public string Reason { get; set; } = string.Empty;

        public OutreachChannel? ChannelOverride { get; set; }

        public string? IntentOverride { get; set; }

        public string? CustomInstruction { get; set; }

        public bool Excluded { get; set; }

        public bool SkipFutureSuggestions { get; set; }
    }
}
