using Entities;

namespace ServiceContracts.DTOs.OutreachDTOs
{
    public class MemberOverrideRequest
    {
        public OutreachChannel? ChannelOverride { get; set; }

        public bool ClearChannelOverride { get; set; }

        public string? IntentOverride { get; set; }

        public string? CustomInstruction { get; set; }

        public bool Excluded { get; set; }

        public bool SkipFutureSuggestions { get; set; }
    }
}
