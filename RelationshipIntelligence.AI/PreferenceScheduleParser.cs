using System;
using System.Text.RegularExpressions;

namespace RelationshipIntelligence.AI
{
    /// <summary>
    /// Deterministic translator from human schedule phrases to supported day
    /// intervals. Anything outside the supported set returns null so Copilot
    /// rejects it honestly instead of inventing a schedule. No LLM, no I/O.
    /// </summary>
    public static class PreferenceScheduleParser
    {
        public static int? ParseReminder(string schedule)
        {
            var text = (schedule ?? string.Empty).Trim().ToLowerInvariant();
            if (text.Length == 0)
                return null;

            if (Regex.IsMatch(text, @"\bdaily\b|\bevery day\b|\beach day\b"))
                return 1;
            if (Regex.IsMatch(text, @"\btwice a month\b|\btwo times a month\b|\bevery two weeks\b|\bf(ö|o)rtnight"))
                return 15;
            if (Regex.IsMatch(text, @"\bweekly\b|\bevery week\b|\bonce a week\b"))
                return 7;
            if (Regex.IsMatch(text, @"\bmonthly\b|\bevery month\b|\bonce a month\b"))
                return 30;

            var every = Regex.Match(text, @"every\s+(\d{1,3})\s+days?");
            if (every.Success && int.TryParse(every.Groups[1].Value, out var days))
                return days is >= 1 and <= 365 ? days : null;

            var bare = Regex.Match(text, @"^(\d{1,3})\s*days?$");
            if (bare.Success && int.TryParse(bare.Groups[1].Value, out var bareDays))
                return bareDays is >= 1 and <= 365 ? bareDays : null;

            return text switch
            {
                "day" => 1,
                "3 days" => 3,
                "7 days" => 7,
                "10 days" => 10,
                "14 days" => 14,
                "month" => 30,
                _ => null
            };
        }
    }
}
