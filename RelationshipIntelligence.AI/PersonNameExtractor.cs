using System;
using System.Collections.Generic;
using System.Linq;

namespace RelationshipIntelligence.AI
{
    public static class PersonNameExtractor
    {
        private static readonly HashSet<string> NonNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "Tell", "What", "Who", "When", "How", "Why", "Prepare", "Find", "Show", "Give",
            "About", "With", "There", "Here", "This", "That", "They", "Them", "Actually",
            "Forget", "Please", "Thanks", "Hello", "Hey", "Hi", "Okay", "Well", "Now",
            "Then", "Also", "Just", "Don't", "Does", "Are", "Can", "Could", "Should"
        };

        /// <summary>
        /// First capitalized non-initial token that is not a discourse word.
        /// Sentence-initial capitalization carries no signal in English.
        /// Returns null when the message names nobody.
        /// </summary>
        public static string? ExtractUnknownName(string message)
        {
            var first = true;
            foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(message, @"\b[A-Z][a-z]{2,}\b"))
            {
                if (first)
                {
                    first = false;
                    continue;
                }
                if (!NonNames.Contains(match.Value))
                    return match.Value;
            }
            return null;
        }

        public static bool DismissesPerson(string lower) =>
            lower.Contains("forget") || lower.Contains("never mind") || lower.Contains("drop it");
    }

    /// <summary>
    /// Builds distinguishable choice labels for same-name contacts: the name
    /// plus organization/role context, with a numbered fallback when even the
    /// context is identical. Matching is always case-insensitive.
    /// </summary>
    public static class PersonChoiceLabels
    {
        public static List<(Guid Id, string Label)> Build(IReadOnlyList<(Guid Id, string Name, string? Org, string? Role)> candidates)
        {
            var labels = candidates.Select(c =>
            {
                var context = string.Join(", ", new[] { c.Role, c.Org }
                    .Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!.Trim()));
                return (c.Id, Label: string.IsNullOrWhiteSpace(context) ? c.Name : $"{c.Name} — {context}");
            }).ToList();
            var dupes = labels.GroupBy(l => l.Label, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1).SelectMany((g, gi) => g.Select((l, i) => (l, i))).ToList();
            foreach (var (l, i) in dupes)
            {
                var idx = labels.FindIndex(x => x.Id == l.Id && string.Equals(x.Label, l.Label, StringComparison.OrdinalIgnoreCase));
                if (idx >= 0)
                    labels[idx] = (l.Id, $"{l.Label} ({i + 1})");
            }
            return labels;
        }

        public static Guid? MatchChoice(IReadOnlyList<(Guid Id, string Label)> options, string message)
        {
            var text = NormalizeChoice(message);
            var exact = options.FirstOrDefault(o => NormalizeChoice(o.Label) == text);
            if (exact != default)
                return exact.Id;
            var byName = options.Where(o => NormalizeChoice(o.Label).StartsWith(text, StringComparison.Ordinal)).ToList();
            if (byName.Count == 1)
                return byName[0].Id;
            return null;
        }

        // Choice matching ignores case, dash style (hyphen/en/em), and extra
        // spaces, so a retyped pick still resolves on the first try.
        private static string NormalizeChoice(string value)
        {
            var s = (value ?? string.Empty).Trim().ToLowerInvariant()
                .Replace('—', '-').Replace('–', '-');
            return System.Text.RegularExpressions.Regex.Replace(s, @"\s+", " ");
        }
    }
}
