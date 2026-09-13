using Entities;
using ServiceContracts;
using ServiceContracts.DTOs.MeetingDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RelationshipIntelligence.AI
{
    /// <summary>
    /// Deterministic meeting extractor used when Copilot:Mode is Stub.
    /// Splits source text into lines and derives topics, questions, and name
    /// mentions with transparent rules — no language model involved.
    /// </summary>
    public sealed class StubMeetingExtractor : IMeetingExtractor
    {
        public Task<MeetingExtraction> ExtractAsync(MeetingExtractionInput input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            var source = string.Join("\n",
                new[] { input.Description, input.Transcript, input.Notes }
                    .Where(s => !string.IsNullOrWhiteSpace(s)));
            var lines = source
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .Where(l => l.Length > 0)
                .Take(60)
                .ToList();

            var knownNames = (input.KnownPeople ?? new List<KnownMeetingPerson>())
                .Select(p => p.Name)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToList();

            var detected = knownNames
                .Where(n => lines.Any(l => l.Contains(n, StringComparison.OrdinalIgnoreCase)))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var findings = new List<ExtractedFinding>();
            foreach (var line in lines)
            {
                var owner = knownNames.FirstOrDefault(n => line.Contains(n, StringComparison.OrdinalIgnoreCase));
                if (line.Contains('?'))
                {
                    findings.Add(new ExtractedFinding
                    {
                        Kind = FindingKind.Question,
                        Title = Truncate(line, 200),
                        PersonName = owner,
                        SourceExcerpt = Truncate(line, 500)
                    });
                }
                else if (line.Contains("agreed", StringComparison.OrdinalIgnoreCase)
                    || line.Contains("will ", StringComparison.OrdinalIgnoreCase)
                    || line.Contains("promise", StringComparison.OrdinalIgnoreCase))
                {
                    findings.Add(new ExtractedFinding
                    {
                        Kind = FindingKind.Commitment,
                        Title = Truncate(line, 200),
                        PersonName = owner,
                        SourceExcerpt = Truncate(line, 500)
                    });
                }
                else
                {
                    findings.Add(new ExtractedFinding
                    {
                        Kind = FindingKind.Topic,
                        Title = Truncate(line, 200),
                        PersonName = owner,
                        SourceExcerpt = Truncate(line, 500)
                    });
                }
            }

            return Task.FromResult(new MeetingExtraction
            {
                Summary = $"Stub analysis of '{input.Title}': {lines.Count} statement(s), {detected.Count} known name(s) mentioned.",
                Topics = lines.Take(10).Select(l => Truncate(l, 200)).ToList(),
                Decisions = new List<string>(),
                Findings = findings.Take(30).ToList(),
                DetectedPeople = detected
            });
        }

        private static string Truncate(string value, int max) =>
            value.Length <= max ? value : value[..max];
    }
}
