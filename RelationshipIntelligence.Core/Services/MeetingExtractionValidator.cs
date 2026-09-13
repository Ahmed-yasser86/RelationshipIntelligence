using Entities;
using ServiceContracts.DTOs.MeetingDTOs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Servicess
{
    /// <summary>
    /// Deterministic guard over AI meeting extraction. Rejects malformed output
    /// before anything reaches the database. No model calls here.
    /// </summary>
    public static class MeetingExtractionValidator
    {
        private static readonly string[] ValidKinds = Enum.GetNames(typeof(FindingKind));

        public static MeetingExtraction Validate(MeetingExtraction? extraction)
        {
            if (extraction == null)
                throw new InvalidOperationException("Meeting extraction produced no output.");

            var summary = (extraction.Summary ?? string.Empty).Trim();
            if (summary.Length == 0)
                throw new InvalidOperationException("Meeting extraction is missing a summary.");

            var findings = new List<ExtractedFinding>();
            foreach (var finding in extraction.Findings ?? Enumerable.Empty<ExtractedFinding>())
            {
                if (finding == null) continue;
                var title = (finding.Title ?? string.Empty).Trim();
                if (title.Length == 0) continue;
                if (title.Length > 200) title = title[..200];
                if (!ValidKinds.Contains(finding.Kind.ToString())) continue;

                findings.Add(new ExtractedFinding
                {
                    Kind = finding.Kind,
                    Title = title,
                    Detail = Clean(finding.Detail, 2000),
                    PersonName = Clean(finding.PersonName, 200),
                    SourceExcerpt = Clean(finding.SourceExcerpt, 500)
                });
            }

            return new MeetingExtraction
            {
                Summary = summary.Length > 4000 ? summary[..4000] : summary,
                Topics = CleanList(extraction.Topics, 200, 30),
                Decisions = CleanList(extraction.Decisions, 500, 30),
                Findings = findings.Take(100).ToList(),
                DetectedPeople = CleanList(extraction.DetectedPeople, 200, 50)
            };
        }

        private static string? Clean(string? value, int max)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var clean = value.Trim();
            return clean.Length > max ? clean[..max] : clean;
        }

        private static List<string> CleanList(IEnumerable<string>? values, int maxEach, int maxCount)
        {
            return (values ?? Enumerable.Empty<string>())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v.Trim())
                .Where(v => v.Length > 0)
                .Select(v => v.Length > maxEach ? v[..maxEach] : v)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(maxCount)
                .ToList();
        }
    }
}
