using Entities;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Servicess;
using ServiceContracts;
using ServiceContracts.DTOs.CopilotDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RelationshipIntelligence.AI
{
    public sealed class CopilotService : ICopilotService
    {
        private readonly KernelFactory _kernels;
        private readonly IRelationshipScoringService _scoring;
        private readonly IInteractionService _interactions;
        private readonly IRelationshipMemoryService _memory;
        private readonly IEventService _events;
        private readonly INetworkAnalysisService _network;
        private readonly IPersonSearcherService _searcher;
        private readonly IPersonGetterService _persons;
        private readonly ILogger<CopilotService> _logger;

        public CopilotService(
            KernelFactory kernels,
            IRelationshipScoringService scoring,
            IInteractionService interactions,
            IRelationshipMemoryService memory,
            IEventService events,
            INetworkAnalysisService network,
            IPersonSearcherService searcher,
            IPersonGetterService persons,
            ILogger<CopilotService> logger)
        {
            _kernels = kernels;
            _scoring = scoring;
            _interactions = interactions;
            _memory = memory;
            _events = events;
            _network = network;
            _searcher = searcher;
            _persons = persons;
            _logger = logger;
        }

        private async Task<(Kernel Kernel, IChatCompletionService Chat)> KernelWithToolsAsync()
        {
            var plugin = new RelationshipPlugin(_scoring, _interactions, _memory, _events, _network, _searcher);
            var kernel = await _kernels.CreateAsync(plugin);
            return (kernel, kernel.GetRequiredService<IChatCompletionService>());
        }

        private static OpenAIPromptExecutionSettings Settings() => new()
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
            Temperature = 0.2,
            MaxTokens = 1500
        };

        private async Task<string> ChatAsync(string userContent, List<ChatTurnDto>? history)
        {
            var (kernel, chat) = await KernelWithToolsAsync();
            var chatHistory = new ChatHistory(CopilotPrompts.System);
            if (history != null)
            {
                foreach (var turn in history.TakeLast(8))
                {
                    if (string.Equals(turn.Role, "assistant", StringComparison.OrdinalIgnoreCase))
                        chatHistory.AddAssistantMessage(turn.Text);
                    else
                        chatHistory.AddUserMessage(turn.Text);
                }
            }
            chatHistory.AddUserMessage(userContent);

            try
            {
                var result = await chat.GetChatMessageContentAsync(chatHistory, Settings(), kernel);
                return (result.Content ?? string.Empty).Trim();
            }
            catch (CopilotNotConfiguredException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Co-pilot model call failed");
                throw CopilotErrors.FromModelFailure(ex);
            }
        }

        private async Task<string> PersonContextBlockAsync(Guid personId)
        {
            var person = await _persons.GetPersonByPersonId(personId);
            if (person == null)
                return $"PERSON: unknown id {personId}.";

            var queue = await _scoring.GetQueueAsync(200);
            var state = queue.FirstOrDefault(q => q.PersonId == personId);
            var interactions = await _interactions.ListForPersonAsync(personId);
            var memory = await _memory.ListForPersonAsync(personId);
            var events = await _events.ListForPersonAsync(personId);

            var sb = new StringBuilder();
            sb.AppendLine($"PERSON: {person.Name} (id {personId}).");
            if (!string.IsNullOrWhiteSpace(person.Origin)) sb.AppendLine($"HOW MET: {person.Origin}");
            if (state != null)
                sb.AppendLine($"DERIVED STATE: band {state.Band}, urgency {Math.Round(state.UrgencyScore)}/100, " +
                    $"cadence ~{(state.CadenceReferenceDays == null ? "unknown" : Math.Round(state.CadenceReferenceDays.Value) + "d")}, " +
                    $"last contact {(state.LastContactAtUtc == null ? "never" : state.LastContactAtUtc.Value.ToString("yyyy-MM-dd"))}, " +
                    $"interactions {state.InteractionCount}, evidence {state.EvidenceStatus}, bridge {state.IsBridge}.");
            else
                sb.AppendLine("DERIVED STATE: none — unscored, no rhythm measured.");
            sb.AppendLine("OBSERVED INTERACTIONS (newest last):");
            foreach (var i in interactions.OrderBy(i => i.TimeOfInteraction).TakeLast(15))
                sb.AppendLine($"- {i.TimeOfInteraction:yyyy-MM-dd} [{i.InteractionType}] {i.InteractionTitle}" +
                    (string.IsNullOrWhiteSpace(i.InteractionDescription) ? "" : $" — {i.InteractionDescription}"));
            if (interactions.Count == 0) sb.AppendLine("- none recorded.");
            sb.AppendLine("USER MEMORY (canonical truth, with provenance):");
            foreach (var m in memory.Where(m => m.Status == 0))
                sb.AppendLine($"- [{m.Kind}/{m.Provenance}] {m.Title}" +
                    (string.IsNullOrWhiteSpace(m.Detail) ? "" : $" — {m.Detail}"));
            if (!memory.Any(m => m.Status == 0)) sb.AppendLine("- none recorded.");
            sb.AppendLine("RECORDED EVENTS:");
            foreach (var e in events)
                sb.AppendLine($"- [{e.Type}] {e.Title} on {e.OccursOn:yyyy-MM-dd} (yearly: {e.RepeatsYearly}, importance {e.Importance})");
            if (events.Count == 0) sb.AppendLine("- none recorded.");
            return sb.ToString();
        }

        public async Task<CopilotAnswer> AskAsync(string question, Guid? personId, List<ChatTurnDto>? history)
        {
            if (string.IsNullOrWhiteSpace(question))
                throw new ArgumentException("Question is required.", nameof(question));

            var context = personId == null
                ? await GeneralContextBlockAsync()
                : await PersonContextBlockAsync(personId.Value);
            var text = await ChatAsync($"CONTEXT:\n{context}\n\nQUESTION: {question.Trim()}", history);

            var citations = new List<CopilotCitation>();
            if (personId != null)
            {
                var person = await _persons.GetPersonByPersonId(personId);
                citations.Add(new CopilotCitation { Kind = "person", Id = personId, Label = person?.Name ?? "contact" });
            }
            return new CopilotAnswer { Text = text, Citations = citations };
        }

        public async Task<CopilotAnswer> SummarizePersonAsync(Guid personId)
        {
            var context = await PersonContextBlockAsync(personId);
            var text = await ChatAsync(
                $"CONTEXT:\n{context}\n\nWrite a relationship briefing: who this person is, how the relationship " +
                "started, current state, recent pattern, upcoming events, open commitments/goals, and what deserves " +
                "attention. Prioritize action-relevant information. Label Observed / Derived / Suggested.", null);

            var person = await _persons.GetPersonByPersonId(personId);
            return new CopilotAnswer
            {
                Text = text,
                Citations = new List<CopilotCitation>
                {
                    new() { Kind = "person", Id = personId, Label = person?.Name ?? "contact" }
                }
            };
        }

        private async Task<string> GeneralContextBlockAsync()
        {
            var queue = await _scoring.GetQueueAsync(10);
            var upcoming = await _events.GetUpcomingAsync(21);
            var sb = new StringBuilder();
            sb.AppendLine("ATTENTION QUEUE (deterministic ranking, top 10):");
            foreach (var q in queue)
                sb.AppendLine($"- {q.Name} (id {q.PersonId}): band {q.Band}, urgency {Math.Round(q.UrgencyScore)}, " +
                    $"last contact {(q.LastContactAtUtc == null ? "never" : q.LastContactAtUtc.Value.ToString("yyyy-MM-dd"))}, " +
                    $"events: {(q.UpcomingEvents.Count == 0 ? "none" : string.Join("; ", q.UpcomingEvents.Select(e => $"{e.Title} in {e.InDays}d")))}");
            if (queue.Count == 0) sb.AppendLine("- queue empty.");
            sb.AppendLine("UPCOMING EVENTS (21d):");
            foreach (var e in upcoming.Take(10))
                sb.AppendLine($"- {e.PersonName ?? "contact"}: {e.Title} in {e.InDays}d");
            if (upcoming.Count == 0) sb.AppendLine("- none.");
            return sb.ToString();
        }

        public async Task<BriefingDto> BuildBriefingAsync()
        {
            var queue = await _scoring.GetQueueAsync(10);
            var upcoming = await _events.GetUpcomingAsync(21);
            var now = DateTime.UtcNow;

            var briefing = new BriefingDto
            {
                GeneratedAtUtc = now,
                AttentionNow = queue.Take(5).Select(q => new BriefingAttentionItem
                {
                    PersonId = q.PersonId,
                    Name = q.Name,
                    Band = q.Band,
                    Reason = $"Urgency {Math.Round(q.UrgencyScore)}/100 ({q.Band}). " +
                        (q.LastContactAtUtc == null ? "No contact recorded." :
                        $"Last contact {q.LastContactAtUtc.Value:yyyy-MM-dd}" +
                        (q.CadenceReferenceDays == null ? "." : $" vs ~{Math.Round(q.CadenceReferenceDays.Value)}d rhythm."))
                }).ToList(),
                UpcomingEvents = upcoming.Take(8).Select(e => new BriefingEventItem
                {
                    PersonId = e.PersonId,
                    PersonName = e.PersonName,
                    Title = e.Title,
                    InDays = e.InDays,
                    SilenceLine = SilenceLineFor(queue, e.PersonId)
                }).ToList()
            };

            var activeCommitments = await ActiveCommitmentsAsync();
            briefing.FollowUpsDue = activeCommitments.Take(8).ToList();

            briefing.SuggestedActions = queue.Take(5).Select(q =>
            {
                var silent = q.SilenceDays ?? TieDecayModel.SilenceDays(q.LastContactAtUtc, now);
                var soon = upcoming.FirstOrDefault(e => e.PersonId == q.PersonId && e.InDays <= 7);
                return soon != null
                    ? $"Reconnect with {q.Name} before {soon.Title} ({(soon.InDays == 0 ? "today" : $"in {soon.InDays}d")}) — {q.Band}, quiet {silent}d."
                    : $"Check in with {q.Name} — {q.Band} (urgency {Math.Round(q.UrgencyScore)}), quiet {silent}d.";
            }).ToList();
            briefing.Changes = queue
                .Where(q => q.SilenceQuantile != null)
                .OrderByDescending(q => q.UrgencyScore)
                .Take(4)
                .Select(q => new BriefingChangeItem
                {
                    PersonId = q.PersonId,
                    Name = q.Name,
                    Direction = (q.SilenceQuantile ?? 0) > 0.5 ? "drifting" : "steady",
                    Detail = $"Quiet {(q.SilenceDays ?? TieDecayModel.SilenceDays(q.LastContactAtUtc, now))}d, " +
                        $"longer than {Math.Round((q.SilenceQuantile ?? 0) * 100)}% of past gaps."
                }).ToList();

            try
            {
                briefing.Summary = await ChatAsync(
                    "Write a 2-3 sentence plain-language briefing from this data. Do not invent people or urgency.\n" +
                    $"ATTENTION: {string.Join("; ", briefing.AttentionNow.Select(a => $"{a.Name} ({a.Band})"))}\n" +
                    $"EVENTS: {string.Join("; ", briefing.UpcomingEvents.Select(e => $"{e.PersonName}: {e.Title} in {e.InDays}d"))}\n" +
                    $"FOLLOW-UPS: {string.Join("; ", briefing.FollowUpsDue)}", null);
            }
            catch (CopilotUnavailableException)
            {
                briefing.Summary = briefing.AttentionNow.Count == 0 && briefing.UpcomingEvents.Count == 0
                    ? "A quiet network — nothing outside its rhythm and no dates approaching."
                    : $"{briefing.AttentionNow.Count} relationship(s) need attention; {briefing.UpcomingEvents.Count} event(s) approaching.";
            }

            return briefing;
        }

        private static string SilenceLineFor(List<ServiceContracts.DTOs.RelationshipHealthResponse> queue, Guid personId)
        {
            var row = queue.FirstOrDefault(q => q.PersonId == personId);
            if (row?.LastContactAtUtc == null) return "no contact recorded";
            var days = row.SilenceDays ?? TieDecayModel.SilenceDays(row.LastContactAtUtc, DateTime.UtcNow);
            return days == null || days <= 0 ? "in touch today" : $"quiet for {days}d";
        }

        private async Task<List<string>> ActiveCommitmentsAsync()
        {
            var people = await _persons.GetAllPersons();
            var result = new List<string>();
            foreach (var person in people.Where(p => p != null).Take(200))
            {
                List<ServiceContracts.DTOs.MemoryDTOs.MemoryEntryResponse> entries;
                try
                {
                    entries = await _memory.ListForPersonAsync(person!.PersonId);
                }
                catch (KeyNotFoundException)
                {
                    continue;
                }
                result.AddRange(entries
                    .Where(e => e.Status == 0 && (e.Kind == Entities.RelationshipMemoryKind.Commitment || e.Kind == Entities.RelationshipMemoryKind.Goal))
                    .Select(e => $"{person!.Name}: {e.Title}"));
                if (result.Count >= 20) break;
            }
            return result;
        }

        private sealed class PlanStepDto
        {
            public string Action { get; set; } = string.Empty;
            public string Why { get; set; } = string.Empty;
            public string Outcome { get; set; } = string.Empty;
        }

        private sealed class PlanJsonDto
        {
            public string Summary { get; set; } = string.Empty;
            public List<PlanStepDto> Steps { get; set; } = new();
        }

        public async Task<PlanSuggestionDto> SuggestPlanAsync(Guid personId, Guid? intentEntryId)
        {
            var context = await PersonContextBlockAsync(personId);
            var entries = await _memory.ListForPersonAsync(personId);
            var intent = intentEntryId != null
                ? entries.FirstOrDefault(e => e.MemoryEntryId == intentEntryId)
                : entries.FirstOrDefault(e => e.Status == 0 && (e.Kind == Entities.RelationshipMemoryKind.Intent || e.Kind == Entities.RelationshipMemoryKind.Goal));

            var text = await ChatAsync(
                $"CONTEXT:\n{context}\n\nUSER INTENT: {(intent == null ? "none stated — give general rhythm guidance, do not invent goals." : $"{intent.Kind}: {intent.Title}" + (string.IsNullOrWhiteSpace(intent.Detail) ? "" : $" — {intent.Detail}"))}\n\n" +
                "Suggest a relationship plan as JSON ONLY, exactly this shape and nothing else: " +
                "{\"summary\": \"one warm sentence framing the plan\", \"steps\": [{\"action\": \"what to do, plain human words\", \"why\": \"why now, citing the actual data above\", \"outcome\": \"what this achieves\"}]}. " +
                "Two to four steps. Human voice throughout: no band/score jargon, no tool names, no section labels. The user decides; you only recommend.", null);

            var person = await _persons.GetPersonByPersonId(personId);
            var actions = ParsePlanSteps(text, personId, person?.Name);
            if (actions.Count == 0)
                actions.Add(await FallbackPlanActionAsync(personId, person?.Name, text));
            return new PlanSuggestionDto
            {
                PersonId = personId,
                Intent = intent?.Title ?? "Maintain a healthy rhythm",
                SuggestedActions = actions,
                LimitedContext = entries.Count == 0
            };
        }

        private static List<PlanActionDto> ParsePlanSteps(string text, Guid personId, string? personName)
        {
            try
            {
                var json = ExtractJson(text.Trim());
                var parsed = JsonSerializer.Deserialize<PlanJsonDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var steps = (parsed?.Steps ?? new List<PlanStepDto>())
                    .Where(s => !string.IsNullOrWhiteSpace(s.Action))
                    .Take(4)
                    .Select(s => new PlanActionDto
                    {
                        Action = s.Action.Trim(),
                        PersonId = personId,
                        PersonName = personName,
                        WhyNow = string.IsNullOrWhiteSpace(s.Why) ? "Based on the relationship context above." : s.Why.Trim(),
                        Outcome = string.IsNullOrWhiteSpace(s.Outcome) ? string.Empty : s.Outcome.Trim()
                    })
                    .ToList();
                if (!string.IsNullOrWhiteSpace(parsed?.Summary))
                    steps.Insert(0, new PlanActionDto
                    {
                        Action = parsed!.Summary.Trim(),
                        PersonId = personId,
                        PersonName = personName,
                        WhyNow = "The framing for the steps below.",
                        Outcome = string.Empty
                    });
                return steps;
            }
            catch (Exception)
            {
                return new List<PlanActionDto>();
            }
        }

        /// <summary>
        /// When the model does not return the plan contract, keep its prose
        /// but ground the "why" deterministically instead of a placeholder.
        /// </summary>
        private async Task<PlanActionDto> FallbackPlanActionAsync(Guid personId, string? personName, string text)
        {
            var why = "Based on the relationship context above.";
            try
            {
                var queue = await _scoring.GetQueueAsync(200);
                var state = queue.FirstOrDefault(q => q.PersonId == personId);
                if (state != null)
                {
                    var silent = state.SilenceDays ?? TieDecayModel.SilenceDays(state.LastContactAtUtc, DateTime.UtcNow);
                    why = $"{state.Band} at urgency {Math.Round(state.UrgencyScore)}" +
                        (silent == null ? "." : $", quiet {silent}d") +
                        (state.CadenceReferenceDays == null ? "." : $" against a ~{Math.Round(state.CadenceReferenceDays.Value)}d rhythm.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Plan fallback state lookup skipped.");
            }
            return new PlanActionDto
            {
                Action = text.Trim(),
                PersonId = personId,
                PersonName = personName,
                WhyNow = why,
                Outcome = string.Empty
            };
        }

        public async Task<BatchIntentDto> ParseOutreachIntentAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new BatchIntentDto
                {
                    NeedsClarification = true,
                    ClarificationPrompt = "Tell me who you want to reach — for example: people I should reconnect with, follow-ups after meetings, or everyone in my attention queue."
                };

            try
            {
                var (kernel, chat) = await KernelWithToolsAsync();
                var history = new ChatHistory(CopilotPrompts.OutreachIntentParser);
                history.AddUserMessage(text.Trim());
                var result = await chat.GetChatMessageContentAsync(history, new OpenAIPromptExecutionSettings
                {
                    Temperature = 0,
                    MaxTokens = 400
                }, kernel);
                var json = ExtractJson((result.Content ?? string.Empty).Trim());
                var parsed = JsonSerializer.Deserialize<BatchIntentDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return ValidateIntent(parsed!, text);
            }
            catch (CopilotNotConfiguredException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Outreach intent parsing failed outright");
                throw CopilotErrors.FromModelFailure(ex);
            }
        }

        /// <summary>
        /// Sanitizes model-produced intent: allowlisted signals, valid channels,
        /// clamped windows. Invalid output asks for clarification — never a
        /// silent keyword guess.
        /// </summary>
        private static BatchIntentDto ValidateIntent(BatchIntentDto? candidate, string originalText)
        {
            var signals = (candidate?.SignalFilters ?? new List<string>())
                .Where(s => BatchIntentDto.KnownSignals.Contains(s))
                .Distinct()
                .ToList();
            if (signals.Count == 0)
                return new BatchIntentDto
                {
                    NeedsClarification = true,
                    ClarificationPrompt = "I could not tell which relationships you mean. Try: reconnect, follow up after meetings, neglected, attention queue, upcoming events, or everyone at an organization."
                };

            string? channel = candidate!.Channel;
            var validChannels = new[] { "Email", "LinkedIn", "Text", "CallPrep" };
            if (channel != null && !validChannels.Contains(channel))
                channel = null;

            var company = string.IsNullOrWhiteSpace(candidate.CompanyName)
                ? null
                : candidate.CompanyName.Trim().Length > 100 ? candidate.CompanyName.Trim()[..100] : candidate.CompanyName.Trim();
            return new BatchIntentDto
            {
                SignalFilters = signals,
                CompanyName = company,
                Channel = channel,
                IntentText = string.IsNullOrWhiteSpace(candidate.IntentText) ? originalText.Trim() : candidate.IntentText.Trim(),
                TimeWindowDays = Math.Clamp(candidate.TimeWindowDays <= 0 ? 7 : candidate.TimeWindowDays, 1, 60)
            };
        }

        public async Task<DraftCommunicationResult> DraftCommunicationAsync(DraftCommunicationRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (request.Person == null)
                throw new ArgumentException("Person context is required.", nameof(request));

            var channelShape = request.Kind == Entities.DraftKind.CallPrep
                ? "a call-preparation brief with exactly these sections: Before the call / During the call / After the call"
                : request.Channel switch
                {
                    Entities.OutreachChannel.Email => "an email with a Subject line and a structured body",
                    Entities.OutreachChannel.LinkedIn => "a concise LinkedIn message (no subject, natural and brief)",
                    Entities.OutreachChannel.Text => "a short conversational text message (two sentences or fewer)",
                    _ => "an email with a Subject line and a structured body"
                };

            var context = new StringBuilder();
            var p = request.Person;
            context.AppendLine($"PERSON: {p.Name}.");
            if (!string.IsNullOrWhiteSpace(p.Band))
                context.AppendLine($"STATE: band {p.Band}" +
                    (p.UrgencyScore == null ? "." : $", urgency {Math.Round(p.UrgencyScore.Value)}."));
            if (!string.IsNullOrWhiteSpace(p.CadenceLine))
                context.AppendLine($"RHYTHM: {p.CadenceLine}");
            foreach (var i in p.RecentInteractions.Take(5)) context.AppendLine($"INTERACTION: {i}");
            foreach (var m in p.MemoryHighlights.Take(6)) context.AppendLine($"MEMORY: {m}");
            foreach (var e in p.UpcomingEvents.Take(3)) context.AppendLine($"EVENT: {e}");
            foreach (var c in p.OpenCommitments.Take(5)) context.AppendLine($"COMMITMENT: {c}");
            foreach (var s in p.CommunicationStyle.Take(4)) context.AppendLine($"STYLE: {s}");
            foreach (var n in p.StyleNotes.Take(4)) context.AppendLine($"STYLE: {n}");
            foreach (var x in p.MessageExamples.Take(3)) context.AppendLine($"EXAMPLE: {x}");

            var instruction = string.IsNullOrWhiteSpace(request.CustomInstruction)
                ? request.GlobalInstruction
                : request.CustomInstruction;
            var brief =
                $"Write {channelShape} for the person above. " +
                $"Communication intent: {request.Intent}. " +
                (string.IsNullOrWhiteSpace(instruction) ? "" : $"Global instruction: {instruction.Trim()}. ") +
                "First decide why the user would contact this person now, from strongest to weakest: " +
                "an unresolved commitment to follow up on; an upcoming event to check in about; " +
                "continuing a recent substantive thread; a remembered topic worth raising; " +
                "or, if there is nothing beyond a long silence, a plain human reconnection with no manufactured reason. " +
                "Let that reason shape the CONTENT of the message — different evidence must produce a materially different message. " +
                "Rules: write as the user in first person, natural and concise. " +
                "Match the user's voice: STYLE lines describe how they write to this person — apply them to greeting, length, and tone. " +
                "EXAMPLE lines show actual messages they wrote — mirror their greeting habit, length, and directness. " +
                "STYLE and EXAMPLE govern HOW to write; they never supply facts. " +
                "Ground every specific claim in the supplied context — " +
                "never invent details, dates, commitments, feelings, outcomes, or conversations. " +
                "When context is thin, stay conservative and general. " +
                "Never mention bands, scores, urgency numbers, days-silent counts, rhythm, cadence, or any bracketed evidence label " +
                "(PERSON/STATE/RHYTHM/INTERACTION/MEMORY/EVENT/COMMITMENT/STYLE/EXAMPLE, [Call], [Intent/User], dates like 2026-08-01). " +
                "Never write meta-commentary such as 'based on our relationship history', 'according to our previous interactions', " +
                "'I noticed we haven't spoken in N days', or 'your relationship rhythm suggests'. " +
                "Never enumerate past interactions or paste evidence lines into the message. " +
                "Do not substitute only the name into a template. " +
                "End with two lines: CONTEXT-USED: <semicolon-separated list of the context items you used, or NONE> and " +
                "SUBJECT: <subject for email, else ->.";

            string content;
            try
            {
                var (kernel, chat) = await KernelWithToolsAsync();
                var history = new ChatHistory(CopilotPrompts.System);
                history.AddUserMessage(context + "\n" + brief);
                var result = await chat.GetChatMessageContentAsync(history, new OpenAIPromptExecutionSettings
                {
                    Temperature = 0.4,
                    MaxTokens = 1200
                }, kernel);
                content = (result.Content ?? string.Empty).Trim();
            }
            catch (CopilotNotConfiguredException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Draft generation model call failed");
                throw CopilotErrors.FromModelFailure(ex);
            }

            return ParseDraftOutput(content, request);
        }

        private static DraftCommunicationResult ParseDraftOutput(string content, DraftCommunicationRequest request)
        {
            var lines = content.Split('\n').Select(l => l.Trim()).ToList();
            var contextUsed = new List<string>();
            string? subject = null;
            var bodyLines = new List<string>();

            foreach (var line in lines)
            {
                if (line.StartsWith("CONTEXT-USED:", StringComparison.OrdinalIgnoreCase))
                {
                    var value = line["CONTEXT-USED:".Length..].Trim();
                    if (!string.Equals(value, "NONE", StringComparison.OrdinalIgnoreCase))
                        contextUsed.AddRange(value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                }
                else if (line.StartsWith("SUBJECT:", StringComparison.OrdinalIgnoreCase))
                {
                    var value = line["SUBJECT:".Length..].Trim();
                    if (!string.Equals(value, "-", StringComparison.Ordinal) && value.Length > 0)
                        subject = value.Length > 200 ? value[..200] : value;
                }
                else
                {
                    bodyLines.Add(line);
                }
            }

            var body = string.Join("\n", bodyLines).Trim();
            if (body.Length == 0)
                throw new CopilotUnavailableException("The assistant returned an empty draft.");
            if (body.Length > 4000)
                body = body[..4000];

            return new DraftCommunicationResult
            {
                Subject = request.Channel == Entities.OutreachChannel.Email ? subject : null,
                Body = body,
                ContextUsed = contextUsed.Take(8).ToList(),
                LimitedContext = contextUsed.Count == 0
            };
        }

        private static string ExtractJson(string content)
        {
            var start = content.IndexOf('{');
            var end = content.LastIndexOf('}');
            if (start < 0 || end <= start)
                throw new CopilotUnavailableException("The assistant returned an unparseable outreach interpretation.");
            return content.Substring(start, end - start + 1);
        }
    }
}
