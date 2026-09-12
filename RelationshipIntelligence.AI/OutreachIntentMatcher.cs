using ServiceContracts.DTOs.CopilotDTOs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RelationshipIntelligence.AI
{
    /// <summary>
    /// Deterministic keyword mapping from natural language to BatchIntent.
    /// Used as the stub implementation and as the live fallback. Never selects
    /// people, scores, or priorities — signal filters only.
    /// </summary>
    public static class OutreachIntentMatcher
    {
        public static BatchIntentDto Match(string text)
        {
            var lower = (text ?? string.Empty).ToLowerInvariant();
            var signals = new List<string>();

            if (lower.Contains("attention queue"))
                signals.Add("attentionQueue");
            if (lower.Contains("reconnect") || lower.Contains("drift") || lower.Contains("cooling"))
                signals.Add("outsideCadence");
            if (lower.Contains("neglect"))
                signals.Add("neglected");
            if (lower.Contains("meeting"))
                signals.Add("recentMeetings");
            if (lower.Contains("follow up") || lower.Contains("follow-up") || lower.Contains("followup"))
                signals.Add("pendingCommitments");
            if (lower.Contains("birthday") || lower.Contains("event") || lower.Contains("date") || lower.Contains("occasion"))
                signals.Add("upcomingEvents");
            if (signals.Count == 0)
                return new BatchIntentDto
                {
                    NeedsClarification = true,
                    ClarificationPrompt = "I could not tell which relationships you mean. Try: reconnect, follow up after meetings, neglected, attention queue, or upcoming events."
                };

            string? channel = null;
            if (lower.Contains("linkedin")) channel = "LinkedIn";
            else if (lower.Contains("email")) channel = "Email";
            else if (lower.Contains("text")) channel = "Text";
            else if (lower.Contains("call")) channel = "CallPrep";

            var window = 7;
            if (lower.Contains("two weeks") || lower.Contains("2 weeks")) window = 14;
            else if (lower.Contains("month")) window = 30;

            return new BatchIntentDto
            {
                SignalFilters = signals.Distinct().ToList(),
                Channel = channel,
                IntentText = text.Trim(),
                TimeWindowDays = window
            };
        }

        public static BatchIntentDto Validate(BatchIntentDto candidate, string originalText)
        {
            if (candidate == null)
                return Match(originalText);

            var signals = (candidate.SignalFilters ?? new List<string>())
                .Where(s => BatchIntentDto.KnownSignals.Contains(s))
                .Distinct()
                .ToList();
            if (signals.Count == 0)
                return Match(originalText);

            string? channel = candidate.Channel;
            var validChannels = new[] { "Email", "LinkedIn", "Text", "CallPrep" };
            if (channel != null && !validChannels.Contains(channel))
                channel = null;

            return new BatchIntentDto
            {
                SignalFilters = signals,
                Channel = channel,
                IntentText = string.IsNullOrWhiteSpace(candidate.IntentText) ? originalText.Trim() : candidate.IntentText.Trim(),
                TimeWindowDays = Math.Clamp(candidate.TimeWindowDays <= 0 ? 7 : candidate.TimeWindowDays, 1, 60)
            };
        }
    }
}
