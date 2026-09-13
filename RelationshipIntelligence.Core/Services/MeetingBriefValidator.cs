using System;
using System.Text.Json;

namespace Servicess
{
    /// <summary>
    /// Structural guard for meeting brief JSON. Requires a meeting object and a
    /// participants array; every participant needs a displayName. Optional
    /// sections (history, state, commitments, events, goals, thingsToRemember,
    /// talkingPoints, questionsToAsk) are validated as arrays when present.
    /// Content honesty is the author's responsibility.
    /// </summary>
    public static class MeetingBriefValidator
    {
        private static readonly string[] ParticipantSections =
        {
            "relationshipHistory", "recentTopics", "sharedProjects", "commitments",
            "relevantEvents", "relevantGoals", "thingsToRemember", "talkingPoints",
            "questionsToAsk"
        };

        public static void Validate(string? briefJson)
        {
            if (string.IsNullOrWhiteSpace(briefJson))
                throw new ArgumentException("Brief content is required.", nameof(briefJson));

            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(briefJson);
            }
            catch (JsonException ex)
            {
                throw new ArgumentException("Brief content must be valid JSON.", ex);
            }

            using (doc)
            {
                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                    throw new ArgumentException("Brief content must be a JSON object.", nameof(briefJson));
                if (!doc.RootElement.TryGetProperty("meeting", out var meeting)
                    || meeting.ValueKind != JsonValueKind.Object)
                    throw new ArgumentException("Brief content must contain a meeting object.", nameof(briefJson));
                if (!doc.RootElement.TryGetProperty("participants", out var participants)
                    || participants.ValueKind != JsonValueKind.Array)
                    throw new ArgumentException("Brief content must contain a participants array.", nameof(briefJson));

                foreach (var participant in participants.EnumerateArray())
                {
                    if (participant.ValueKind != JsonValueKind.Object)
                        throw new ArgumentException("Each participant must be an object.", nameof(briefJson));
                    if (!participant.TryGetProperty("displayName", out var name)
                        || name.ValueKind != JsonValueKind.String
                        || string.IsNullOrWhiteSpace(name.GetString()))
                        throw new ArgumentException("Each participant needs a displayName.", nameof(briefJson));
                    foreach (var section in ParticipantSections)
                    {
                        if (participant.TryGetProperty(section, out var value)
                            && value.ValueKind != JsonValueKind.Array
                            && value.ValueKind != JsonValueKind.Null)
                            throw new ArgumentException($"Participant section '{section}' must be an array.", nameof(briefJson));
                    }
                }
            }
        }
    }
}
