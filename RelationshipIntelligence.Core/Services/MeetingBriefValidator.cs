using System;
using System.Text.Json;

namespace Servicess
{
    /// <summary>
    /// Structural guard for meeting brief JSON. Rejects non-objects and documents
    /// missing the participants array. Content honesty is the author's responsibility.
    /// </summary>
    public static class MeetingBriefValidator
    {
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
                if (!doc.RootElement.TryGetProperty("participants", out var participants)
                    || participants.ValueKind != JsonValueKind.Array)
                    throw new ArgumentException("Brief content must contain a participants array.", nameof(briefJson));
            }
        }
    }
}
