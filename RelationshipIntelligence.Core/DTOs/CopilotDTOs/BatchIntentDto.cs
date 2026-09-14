using System.Collections.Generic;

namespace ServiceContracts.DTOs.CopilotDTOs
{
    /// <summary>
    /// Structured interpretation of a natural-language outreach request.
    /// Contains signal filters and preferences ONLY — never person ids, scores,
    /// or priorities. Person selection is always done by the deterministic resolver.
    /// </summary>
    public class BatchIntentDto
    {
        public static readonly string[] KnownSignals =
        {
            "outsideCadence", "recentMeetings", "neglected", "attentionQueue",
            "upcomingEvents", "pendingCommitments", "companyMembers"
        };

        public List<string> SignalFilters { get; set; } = new();

        /// <summary>
        /// Organization for the companyMembers signal, matched server-side
        /// against the user's actual organizations. Never free text into matching.
        /// </summary>
        public string? CompanyName { get; set; }

        public string? Channel { get; set; }

        public string? IntentText { get; set; }

        public int TimeWindowDays { get; set; } = 7;

        public bool NeedsClarification { get; set; }

        public string? ClarificationPrompt { get; set; }
    }
}
