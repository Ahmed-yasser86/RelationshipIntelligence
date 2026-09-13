using Entities;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using ServiceContracts;
using ServiceContracts.DTOs.MeetingDTOs;
using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RelationshipIntelligence.AI
{
    public sealed class MeetingExtractor : IMeetingExtractor
    {
        private const string ExtractionPrompt = """
            You process a meeting into structured relationship intelligence. Respond with EXACTLY this JSON shape and nothing else:
            {"summary": string, "topics": string[], "decisions": string[],
             "findings": [{"kind": "Topic"|"Decision"|"Commitment"|"ActionItem"|"FollowUp"|"Question"|"Event"|"PersonFact"|"Project"|"DateMention",
                           "title": string, "detail": string|null, "personName": string|null, "sourceExcerpt": string|null}],
             "detectedPeople": string[]}
            Rules:
            - Extract only what the source supports. Never invent names, dates, commitments, or facts.
            - personName links a finding to a person named in the meeting; use names exactly as written.
            - sourceExcerpt is a short verbatim quote when one exists, else null.
            - detectedPeople lists every person name mentioned or participating, deduplicated.
            - If the user instructions state a focus, emphasize it WITHOUT changing facts.
            - Respect this canonical memory (user-confirmed truth; do not contradict or re-suggest it as new):
            """;

        private readonly KernelFactory _kernels;
        private readonly ILogger<MeetingExtractor> _logger;

        public MeetingExtractor(KernelFactory kernels, ILogger<MeetingExtractor> logger)
        {
            _kernels = kernels;
            _logger = logger;
        }

        public async Task<MeetingExtraction> ExtractAsync(MeetingExtractionInput input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            var kernel = await _kernels.CreateAsync();
            var chat = kernel.GetRequiredService<IChatCompletionService>();

            var source = new StringBuilder();
            source.AppendLine($"TITLE: {input.Title}");
            source.AppendLine($"DATE: {input.OccurredAtUtc:yyyy-MM-dd}");
            if (!string.IsNullOrWhiteSpace(input.Description))
                source.AppendLine($"DESCRIPTION (user context): {input.Description}");
            if (input.KnownPeople.Count > 0)
                source.AppendLine("KNOWN CONTACTS: " + string.Join("; ", input.KnownPeople.Select(p => p.Name)));
            if (input.ActiveMemoryTitles.Count > 0)
                source.AppendLine("CANONICAL MEMORY (do not re-suggest): " + string.Join("; ", input.ActiveMemoryTitles.Take(50)));
            if (!string.IsNullOrWhiteSpace(input.Transcript))
                source.AppendLine("TRANSCRIPT: " + Truncate(input.Transcript, 40000));
            if (!string.IsNullOrWhiteSpace(input.Notes))
                source.AppendLine("NOTES: " + Truncate(input.Notes, 10000));

            var history = new ChatHistory(ExtractionPrompt + "\n" + string.Join("\n", input.ActiveMemoryTitles.Take(20)));
            if (!string.IsNullOrWhiteSpace(input.UserInstructions))
                history.AddUserMessage("ANALYSIS FOCUS: " + input.UserInstructions.Trim());
            history.AddUserMessage(source.ToString());

            string content;
            try
            {
                var result = await chat.GetChatMessageContentAsync(history, new OpenAIPromptExecutionSettings
                {
                    Temperature = 0.1,
                    MaxTokens = 4000
                }, kernel);
                content = (result.Content ?? string.Empty).Trim();
            }
            catch (CopilotNotConfiguredException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Meeting extraction model call failed");
                throw new CopilotUnavailableException("The assistant could not process the meeting. Check provider settings and try again.", ex);
            }

            var json = ExtractJson(content);
            MeetingExtraction? extraction;
            try
            {
                extraction = JsonSerializer.Deserialize<MeetingExtraction>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException ex)
            {
                throw new CopilotUnavailableException("The assistant returned an unparseable meeting analysis.", ex);
            }

            if (extraction == null)
                throw new CopilotUnavailableException("The assistant returned an empty meeting analysis.");

            return extraction;
        }

        private static string Truncate(string value, int max) =>
            value.Length <= max ? value : value[..max];

        private static string ExtractJson(string content)
        {
            var start = content.IndexOf('{');
            var end = content.LastIndexOf('}');
            if (start < 0 || end <= start)
                throw new CopilotUnavailableException("The assistant returned an unparseable meeting analysis.");
            return content.Substring(start, end - start + 1);
        }
    }
}
