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

        /// <summary>
        /// Typo-tolerant similarity for "did you mean" suggestions. Token-set
        /// based: compares the best-matching token pair so "Dina Smair" still
        /// finds "Dina Samir", while unrelated names score near zero.
        /// Pure function, no I/O. Threshold lives with the caller.
        /// </summary>
        public static double Similarity(string a, string b)
        {
            var at = Tokenize(a);
            var bt = Tokenize(b);
            if (at.Count == 0 || bt.Count == 0)
                return 0;
            // Average of best-match-per-token both directions: tolerant to one
            // mistyped token, strict about wholly different names.
            double Forward(IReadOnlyList<string> x, IReadOnlyList<string> y) =>
                x.Average(t => y.Max(u => TokenSimilarity(t, u)));
            return (Forward(at, bt) + Forward(bt, at)) / 2;
        }

        private static List<string> Tokenize(string value) =>
            System.Text.RegularExpressions.Regex.Matches(value.ToLowerInvariant(), @"[a-z]+")
                .Select(m => m.Value)
                .Where(t => t.Length >= 2)
                .ToList();

        private static double TokenSimilarity(string a, string b)
        {
            if (a == b)
                return 1;
            var distance = Levenshtein(a, b);
            var max = Math.Max(a.Length, b.Length);
            if (max == 0)
                return 1;
            var score = 1.0 - (double)distance / max;
            // Single-edit typos ("smair" vs "samir") score high; require a
            // shared prefix or big overlap so "omar"/"sara" stay near zero.
            if (distance == 1)
                return 0.9;
            if (score < 0.5)
                return 0;
            if (a[0] != b[0])
                return score * 0.5;
            return score;
        }

        private static int Levenshtein(string a, string b)
        {
            var prev = new int[b.Length + 1];
            for (var j = 0; j <= b.Length; j++)
                prev[j] = j;
            for (var i = 1; i <= a.Length; i++)
            {
                var cur = new int[b.Length + 1];
                cur[0] = i;
                for (var j = 1; j <= b.Length; j++)
                    cur[j] = Math.Min(Math.Min(cur[j - 1] + 1, prev[j] + 1),
                        prev[j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
                prev = cur;
            }
            return prev[b.Length];
        }
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
