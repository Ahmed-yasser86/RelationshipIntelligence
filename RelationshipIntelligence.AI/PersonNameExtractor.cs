using System;
using System.Collections.Generic;

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
}
