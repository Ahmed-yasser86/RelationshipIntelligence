using Entities;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Servicess;
using ServiceContracts;
using ServiceContracts.DTOs.AgentDTOs;
using ServiceContracts.DTOs.CopilotDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RelationshipIntelligence.AI
{
    public sealed class CopilotAgent : ICopilotAgent
    {
        private const string GoalPrompt = """
            Route the user's relationship-assistant request to exactly one lane. Respond with EXACTLY this JSON and nothing else:
            {"goal":"greet|answer|act|meeting|outreach|unknown",
             "personRefs":["names mentioned"],
             "isExplicitInstruction":true|false,
             "needsClarification":true|false,
             "clarificationPrompt":"..."|null}
            Lanes (pick one):
            - greet: hellos, capability questions, small talk.
            - answer: any question answered by reading data (who/what/when/why about contacts, orgs, interactions, events, meetings, outreach, memories, queue, digest, reminders, preferences). DEFAULT for questions.
            - act: the message itself orders ONE concrete mutation ("log a call with Salma", "remind me about X every 10 days", "create ..."). isExplicitInstruction true ONLY here.
            - meeting: plan/log/process a meeting (participants, agenda, transcript, brief).
            - outreach: build a contact batch, drafts, approve drafts, channel choice.
            Rules: needsClarification true only when required information is missing (action on nobody, meeting with nobody). Never invent names: personRefs contains ONLY names literally present in the message.
            """;

        private readonly KernelFactory _kernels;
        private readonly IAgentSessionStore _sessions;
        private readonly ICurrentUserService _currentUser;
        private readonly RelationshipQueryPlugin _query;
        private readonly PlanningPlugin _planning;
        private readonly ActionPlugin _actions;
        private readonly IRelationshipScoringService _scoring;
        private readonly IPersonGetterService _persons;
        private readonly ICopilotService _copilot;
        private readonly IOutreachService _outreach;
        private readonly IMeetingService _meetings;
        private readonly ILogger<CopilotAgent> _logger;

        public CopilotAgent(
            KernelFactory kernels,
            IAgentSessionStore sessions,
            ICurrentUserService currentUser,
            RelationshipQueryPlugin query,
            PlanningPlugin planning,
            ActionPlugin actions,
            IRelationshipScoringService scoring,
            IPersonGetterService persons,
            ICopilotService copilot,
            IOutreachService outreach,
            IMeetingService meetings,
            ILogger<CopilotAgent> logger)
        {
            _kernels = kernels;
            _sessions = sessions;
            _currentUser = currentUser;
            _query = query;
            _planning = planning;
            _actions = actions;
            _scoring = scoring;
            _persons = persons;
            _copilot = copilot;
            _outreach = outreach;
            _meetings = meetings;
            _logger = logger;
        }

        private Guid OwnerId()
        {
            var id = _currentUser.UserId;
            if (id == null || id == Guid.Empty)
                throw new UnauthorizedAccessException("The co-pilot requires an authenticated user.");
            return id.Value;
        }

        public async Task<AgentResponse> ChatAsync(AgentChatRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
                throw new ArgumentException("Message is required.", nameof(request));

            var ownerId = OwnerId();
            var session = await _sessions.GetOrCreateAsync(request.SessionId, ownerId);
            AdoptAppContext(session, request.AppContext);

            // A just-made disambiguation pick sets the focus; the agent turn
            // below then answers with the focus already resolved.
            AdoptPendingChoice(session, request.Message.Trim());

            var kernel = await _kernels.CreateAsync();
            kernel.Plugins.AddFromObject(_query, "relationships");
            kernel.Plugins.AddFromObject(_planning, "planning");
            kernel.Plugins.AddFromObject(_actions, "actions");

            // One LLM call routes; a second LLM call (with tools) answers.
            // No keyword/regex routing anywhere in the chat path: every
            // capability the chat offers is an agent tool, and the model
            // picks the tool — including mention resolution below.
            var goal = await ClassifyAsync(kernel, session, request);
            var response = new AgentResponse { SessionId = session.SessionId };

            switch (goal.Goal)
            {
                case "greet":
                    response.Text += "Hi — I keep track of your relationships with you. Ask what is happening with someone, who needs attention, what is coming up, or tell me to prepare messages, a call brief, or a meeting.";
                    response.Actions.Add(new AgentAction { Kind = "ask", Label = "Who needs attention?" });
                    break;
                case "meeting":
                    await HandleMeetingAsync(kernel, session, request, goal, response);
                    break;
                case "outreach":
                    await HandleOutreachFlowAsync(kernel, session, request, goal, response);
                    break;
                case "act":
                    await HandleActionAsync(kernel, session, request, goal, response);
                    break;
                default:
                    await HandleConversationalAsync(kernel, session, request, goal, response);
                    break;
            }

            await _sessions.SaveAsync(ownerId, session);
            response.WorkingState = await WorkingStateAsync(session);
            return response;
        }

        private static void AdoptAppContext(AgentSession session, AgentAppContext context)
        {
            if (context == null) return;
            if (context.PersonId != null && session.PersonId == null)
                session.PersonId = context.PersonId;
            else if (context.PersonId != null && session.PersonId != context.PersonId && session.CurrentTask == null)
                session.PersonId = context.PersonId;
            if (context.SelectedPersonIds.Length > 0 && session.SelectedPersonIds.Count == 0)
                session.SelectedPersonIds.AddRange(context.SelectedPersonIds.Where(id => !session.SelectedPersonIds.Contains(id)));
            if (context.MeetingId != null && session.MeetingId == null)
                session.MeetingId = context.MeetingId;
            if (context.BatchId != null && session.BatchId == null)
                session.BatchId = context.BatchId;
        }

        private sealed class GoalClassification
        {
            public string Goal { get; set; } = "unknown";
            public List<string> PersonRefs { get; set; } = new();
            public bool IsExplicitInstruction { get; set; }
            public bool NeedsClarification { get; set; }
            public string? ClarificationPrompt { get; set; }
        }

        private async Task<GoalClassification> ClassifyAsync(Kernel kernel, AgentSession session, AgentChatRequest request)
        {
            var chat = kernel.GetRequiredService<IChatCompletionService>();
            var history = new ChatHistory(GoalPrompt);
            history.AddUserMessage(
                $"Current task: {session.CurrentTask ?? "none"}. " +
                $"Known person in focus: {(session.PersonId == null ? "none" : session.PersonId.ToString())}. " +
                $"Recent turns: {string.Join(" | ", (request.History ?? new List<ChatTurnDto>()).TakeLast(4).Select(t => $"{t.Role}: {t.Text}"))}\n" +
                $"Message: {request.Message.Trim()}");
            try
            {
                var result = await chat.GetChatMessageContentAsync(history, new OpenAIPromptExecutionSettings
                {
                    Temperature = 0,
                    MaxTokens = 400
                }, kernel);
                var json = ExtractJson((result.Content ?? string.Empty).Trim());
                var parsed = JsonSerializer.Deserialize<GoalClassification>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (parsed != null && !string.IsNullOrWhiteSpace(parsed.Goal))
                    return parsed;
            }
            catch (CopilotNotConfiguredException) { throw; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Goal classification fell back to conversational handling");
            }
            return new GoalClassification { Goal = "unknown" };
        }

        private static string ExtractJson(string content)
        {
            var start = content.IndexOf('{');
            var end = content.LastIndexOf('}');
            if (start < 0 || end <= start)
                throw new CopilotUnavailableException("The assistant returned an unparseable classification.");
            return content.Substring(start, end - start + 1);
        }

        private async Task<List<(Guid Id, string Name, string? Org, string? Role)>> ResolveDetailedCandidatesAsync(string name)
        {
            var raw = await _query.QueryContactsAsync(name: name, top: 5);
            var result = new List<(Guid, string, string?, string?)>();
            try
            {
                using var doc = JsonDocument.Parse(raw);
                if (!doc.RootElement.TryGetProperty("members", out var members)
                    || members.ValueKind != JsonValueKind.Array)
                    return result;
                foreach (var el in members.EnumerateArray().Take(5))
                {
                    if (!el.TryGetProperty("personId", out var idProp) || !el.TryGetProperty("name", out var nameProp))
                        continue;
                    string? org = null;
                    string? role = null;
                    if (el.TryGetProperty("organizations", out var orgs) && orgs.ValueKind == JsonValueKind.Array)
                        org = orgs.EnumerateArray().Select(e => e.GetString()).FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));
                    if (el.TryGetProperty("roles", out var roles) && roles.ValueKind == JsonValueKind.Array)
                        role = roles.EnumerateArray().Select(e => e.GetString()).FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));
                    result.Add((idProp.GetGuid(), nameProp.GetString() ?? "?", org, role));
                }
            }
            catch (JsonException) { }
            return result;
        }

        /// <summary>
        /// Applies a pending disambiguation pick. On a match the focus is set
        /// and routing continues with the focus already resolved, so the name
        /// inside the label is never re-extracted into a second prompt.
        /// Anything else clears the pending pick — a new question is never
        /// trapped behind an old clarification.
        /// </summary>
        private static bool AdoptPendingChoice(AgentSession session, string message)
        {
            if (session.PendingCandidates.Count == 0)
                return false;
            var picked = PersonChoiceLabels.MatchChoice(
                session.PendingCandidates.Select(c => (c.PersonId, c.Label)).ToList(), message);
            session.PendingCandidates.Clear();
            if (picked == null)
                return false;
            session.PersonId = picked.Value;
            session.CurrentTask = null;
            return true;
        }

        private async Task HandleConversationalAsync(Kernel kernel, AgentSession session, AgentChatRequest request, GoalClassification goal, AgentResponse response)
        {
            // No name extraction, no regex, no pre-resolution here: the full
            // request goes to the agent turn with tools. The model resolves
            // people via QueryContacts (and spelling variants when exact
            // search finds nothing), asks for disambiguation itself, and
            // answers from tool results. A wrong guess is the model's to
            // correct on the next turn — never a dead-end string in code.
            var chat = kernel.GetRequiredService<IChatCompletionService>();
            var history = new ChatHistory(CopilotPrompts.System);
            foreach (var turn in (request.History ?? new List<ChatTurnDto>()).TakeLast(8))
            {
                if (string.Equals(turn.Role, "assistant", StringComparison.OrdinalIgnoreCase))
                    history.AddAssistantMessage(turn.Text);
                else
                    history.AddUserMessage(turn.Text);
            }
            var focus = session.PersonId == null
                ? "No person in focus."
                : $"Person in focus: {session.PersonId}. Prefer tools for this person before answering.";
            history.AddUserMessage(focus + "\nUser: " + request.Message.Trim());

            string text;
            try
            {
                var result = await chat.GetChatMessageContentAsync(history, new OpenAIPromptExecutionSettings
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
                    Temperature = 0.3,
                    MaxTokens = 1200
                }, kernel);
                text = (result.Content ?? string.Empty).Trim();
            }
            catch (CopilotNotConfiguredException) { throw; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Agent conversational call failed");
                throw CopilotErrors.FromModelFailure(ex);
            }

            if (string.IsNullOrWhiteSpace(text))
                throw new CopilotUnavailableException("The assistant returned an empty answer.");

            response.Text += text;
            if (session.PersonId != null)
            {
                var person = await _persons.GetPersonByPersonId(session.PersonId);
                response.Citations.Add(new CopilotCitation { Kind = "person", Id = session.PersonId, Label = person?.Name ?? "contact" });
                // Deterministic evidence behind the answer: canonical state for
                // the person in focus, so the drawer always has something to show.
                try
                {
                    var queue = await _scoring.GetQueueAsync(200);
                    var state = queue.FirstOrDefault(q => q.PersonId == session.PersonId);
                    response.Evidence.Add(new AgentEvidence
                    {
                        Title = person?.Name ?? "contact",
                        Detail = state == null
                            ? "No ranked state — too little history to score."
                            : $"Band {state.Band}, urgency {Math.Round(state.UrgencyScore)}, " +
                              $"{state.InteractionCount} interactions" +
                              (state.LastContactAtUtc == null ? ", no contact recorded."
                              : $", quiet {TieDecayModel.SilenceDays(state.LastContactAtUtc, DateTime.UtcNow)}d."),
                        Kind = "derived",
                        RefId = session.PersonId,
                        RefKind = "person"
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Evidence enrichment skipped for person {PersonId}", session.PersonId);
                }
                response.Actions.Add(new AgentAction { Kind = "open", Label = "Open relationship", Payload = session.PersonId.ToString() });
                response.Actions.Add(new AgentAction { Kind = "log", Label = "Log interaction", Payload = session.PersonId.ToString() });
                response.Actions.Add(new AgentAction { Kind = "explain", Label = "Explain score", Payload = session.PersonId.ToString() });
            }
            else
            {
                // Queue-level evidence for unfocused answers so "Why?" always
                // has something truthful to open.
                try
                {
                    var queue = await _scoring.GetQueueAsync(3);
                    foreach (var q in queue)
                    {
                        response.Evidence.Add(new AgentEvidence
                        {
                            Title = q.Name,
                            Detail = $"Band {q.Band}, urgency {Math.Round(q.UrgencyScore)}, {q.InteractionCount} interactions.",
                            Kind = "derived",
                            RefId = q.PersonId,
                            RefKind = "person"
                        });
                        response.Citations.Add(new CopilotCitation { Kind = "person", Id = q.PersonId, Label = q.Name });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Queue evidence enrichment skipped.");
                }
                response.Actions.Add(new AgentAction { Kind = "open-queue", Label = "Open attention queue" });
            }
        }

        private async Task HandleActionAsync(Kernel kernel, AgentSession session, AgentChatRequest request, GoalClassification goal, AgentResponse response)
        {
            if (!goal.IsExplicitInstruction)
            {
                response.Text += "Before I do that, confirm exactly what you want — for example \"log a call with Salma about the proposal\". Nothing changes until you say so.";
                response.NeedsInput = new AgentClarification
                {
                    Prompt = "What exactly should I do?",
                    Options = new List<string>()
                };
                return;
            }

            if (session.PersonId == null && goal.PersonRefs.Count > 0)
            {
                var candidates = await ResolveDetailedCandidatesAsync(goal.PersonRefs[0]);
                if (candidates.Count == 1)
                    session.PersonId = candidates[0].Id;
                // Zero or several: do NOT answer "I don't have" or "which
                // one" here — the agent turn below resolves via QueryContacts
                // (with spelling variants) and asks itself when ambiguous.
            }

            var person = session.PersonId == null
                ? null
                : await _persons.GetPersonByPersonId(session.PersonId);
            var name = person?.Name ?? "contact";
            // No keyword parsing: hand the full request to the agent turn
            // with tools. The model resolves the person via QueryContacts,
            // proposes the concrete mutation, and executes only the
            // confirmation-gated action tools.
            await HandleConversationalAsync(kernel, session, request, goal, response);
            if (session.PersonId != null)
                response.Actions.Add(new AgentAction { Kind = "log", Label = $"Log interaction with {name}", Payload = session.PersonId.ToString() });
        }



        private async Task HandleMeetingAsync(Kernel kernel, AgentSession session, AgentChatRequest request, GoalClassification goal, AgentResponse response)
        {
            if (session.MeetingId == null)
            {
                if (goal.PersonRefs.Count == 0)
                {
                    response.Text += "Who are you meeting? Give me names and I'll map them to your contacts — I won't invent participants.";
                    response.NeedsInput = new AgentClarification { Prompt = "Who are you meeting?", Options = new List<string>() };
                    session.CurrentTask = "meeting-prep";
                    return;
                }

                // Resolve each named participant via the same QueryContacts
                // path the agent uses (exact + spelling variants inside the
                // tool chain). Only the unambiguous single-match fast path
                // stays here so a fresh meeting can start without a model
                // round-trip; zero/multi matches defer to the agent turn,
                // which disambiguates with evidence instead of dead-ending.
                var mapped = new List<Guid>();
                foreach (var name in goal.PersonRefs.Distinct(StringComparer.OrdinalIgnoreCase).Take(10))
                {
                    var candidates = await ResolveDetailedCandidatesAsync(name);
                    if (candidates.Count != 1)
                    {
                        await HandleConversationalAsync(kernel, session, request, goal, response);
                        return;
                    }
                    mapped.Add(candidates[0].Id);
                }

                response.Text += $"I can prepare for a meeting with {string.Join(", ", goal.PersonRefs.Distinct(StringComparer.OrdinalIgnoreCase).Take(10))}. Give me a title and date — for example \"Partnership review tomorrow at 10\" — and I'll create the preparation.";
                session.CurrentTask = "meeting-prep";
                session.SelectedPersonIds.Clear();
                session.SelectedPersonIds.AddRange(mapped);
                session.PendingApprovals.Add("meeting-participants:" + string.Join(",", mapped));
                response.Actions.Add(new AgentAction { Kind = "prepare-meeting", Label = "Start meeting preparation", Payload = string.Join(",", mapped) });
                return;
            }

            await HandleConversationalAsync(kernel, session, request, goal, response);
        }

        private async Task HandleOutreachFlowAsync(Kernel kernel, AgentSession session, AgentChatRequest request, GoalClassification goal, AgentResponse response)
        {
            var message = request.Message.Trim();

            if (session.BatchId == null)
            {
                var parsed = await _copilot.ParseOutreachIntentAsync(message);
                if (parsed.NeedsClarification)
                {
                    response.Text += parsed.ClarificationPrompt ?? "Tell me who you want to reach.";
                    response.NeedsInput = new AgentClarification { Prompt = "Who should be included?", Options = new List<string>() };
                    session.CurrentTask = "outreach";
                    return;
                }

                ServiceContracts.DTOs.OutreachDTOs.OutreachBatchResponse batch;
                try
                {
                    batch = await _outreach.BuildFromSignalsAsync(new ServiceContracts.DTOs.OutreachDTOs.BuildBatchFromSignalsRequest
                    {
                        SignalFilters = parsed.SignalFilters,
                        TimeWindowDays = parsed.TimeWindowDays,
                        MaxMembers = 12,
                        Intent = string.IsNullOrWhiteSpace(parsed.IntentText) ? "Reconnect" : parsed.IntentText,
                        CompanyName = parsed.CompanyName
                    });
                }
                catch (ArgumentException ex)
                {
                    response.Text += ex.Message;
                    return;
                }
                session.BatchId = batch.OutreachBatchId;
                session.CurrentTask = "outreach";
                session.SelectedPersonIds.Clear();
                session.SelectedPersonIds.AddRange(batch.Members.Where(m => !m.Excluded).Select(m => m.PersonId));

                if (batch.Members.Count == 0)
                {
                    response.Text += "No one currently matches those signals. Try a wider window or different signals.";
                    return;
                }

                response.Text += $"I found {batch.Members.Count} people worth considering: " +
                    string.Join("; ", batch.Members.Take(5).Select(m => $"{m.PersonName} — {m.Reason}")) +
                    (batch.Members.Count > 5 ? $", and {batch.Members.Count - 5} more." : ".") +
                    " Tell me who to remove, or say continue to set up the message.";
                foreach (var member in batch.Members.Take(8))
                    response.Citations.Add(new CopilotCitation { Kind = "person", Id = member.PersonId, Label = member.PersonName ?? "contact" });
                response.Actions.Add(new AgentAction { Kind = "open-batch", Label = "Review in outreach", Payload = batch.OutreachBatchId.ToString() });
                return;
            }

            var batchState = await _outreach.GetAsync(session.BatchId.Value);

            if (message.Contains("approve", StringComparison.OrdinalIgnoreCase))
            {
                var reviewable = batchState.Drafts.Where(d => d.Status == Entities.DraftStatus.Draft || d.Status == Entities.DraftStatus.Edited).ToList();
                if (reviewable.Count == 0)
                {
                    response.Text += "There is nothing ready to approve. Generate drafts first, review them, then approve.";
                    return;
                }
                response.Text += $"Ready to approve {reviewable.Count} draft(s): " +
                    string.Join("; ", reviewable.Take(5).Select(d => $"{d.PersonName} via {d.Channel}")) +
                    ". Approving marks them reviewed — nothing sends by itself. Say \"approve now\" to confirm.";
                session.PendingApprovals.Add("approve-batch:" + batchState.OutreachBatchId);
                response.Actions.Add(new AgentAction { Kind = "approve-batch", Label = $"Approve {reviewable.Count} drafts", Payload = batchState.OutreachBatchId.ToString() });
                return;
            }

            if (message.Contains("approve now", StringComparison.OrdinalIgnoreCase)
                && session.PendingApprovals.Any(p => p.StartsWith("approve-batch:")))
            {
                var approved = await _outreach.ApproveAsync(batchState.OutreachBatchId, null);
                session.PendingApprovals.RemoveAll(p => p.StartsWith("approve-batch:"));
                response.Text += $"Approved {approved.Drafts.Count(d => d.Status == Entities.DraftStatus.Approved)} draft(s). Nothing was sent — log each conversation as an interaction when it happens so the relationship learns from it.";
                return;
            }

            if (message.Contains("prepare", StringComparison.OrdinalIgnoreCase) || message.Contains("draft", StringComparison.OrdinalIgnoreCase))
            {
                var withDrafts = await _outreach.GenerateDraftsAsync(batchState.OutreachBatchId);
                response.Text += $"Prepared {withDrafts.Drafts.Count} personalized draft(s), each grounded in that person's context. Review them here or open the batch to edit, regenerate, or approve individually.";
                foreach (var draft in withDrafts.Drafts.Take(5))
                    response.Evidence.Add(new AgentEvidence
                    {
                        Title = $"Draft for {draft.PersonName}",
                        Detail = (draft.Body ?? string.Empty).Length > 220 ? draft.Body.Substring(0, 220) + "…" : draft.Body ?? string.Empty,
                        Kind = "suggested",
                        RefId = draft.CommunicationDraftId,
                        RefKind = "draft"
                    });
                response.Actions.Add(new AgentAction { Kind = "open-batch", Label = "Review drafts", Payload = batchState.OutreachBatchId.ToString() });
                return;
            }

            var channel = DetectChannel(message);
            if (channel != null)
            {
                await _outreach.UpdateSettingsAsync(batchState.OutreachBatchId, new ServiceContracts.DTOs.OutreachDTOs.BatchSettingsRequest
                {
                    Channel = channel.Value,
                    Intent = batchState.Intent
                });
                session.Channel = channel.Value.ToString();
                response.Text += $"Channel set to {channel.Value} for everyone without an individual override. What should the messages aim for — reconnect, follow up, check in?";
                return;
            }

            if (message.Length > 3)
            {
                await _outreach.UpdateSettingsAsync(batchState.OutreachBatchId, new ServiceContracts.DTOs.OutreachDTOs.BatchSettingsRequest
                {
                    Channel = batchState.Channel,
                    Intent = batchState.Intent,
                    GlobalInstruction = message.Trim().Length > 1000 ? message.Trim()[..1000] : message.Trim()
                });
                session.GlobalInstruction = message.Trim();
                response.Text += "Applied that instruction to the whole batch. Say \"prepare\" when ready and I'll draft each message.";
                return;
            }

            await HandleConversationalAsync(kernel, session, request, goal, response);
        }

        private static Entities.OutreachChannel? DetectChannel(string message)
        {
            var lower = message.ToLowerInvariant();
            if (lower.Contains("linkedin")) return Entities.OutreachChannel.LinkedIn;
            if (lower.Contains("email")) return Entities.OutreachChannel.Email;
            if (lower.Contains("text")) return Entities.OutreachChannel.Text;
            if (lower.Contains("call")) return Entities.OutreachChannel.CallPrep;
            return null;
        }

        private async Task<AgentWorkingState> WorkingStateAsync(AgentSession session)
        {
            var state = new AgentWorkingState
            {
                CurrentTask = session.CurrentTask,
                Channel = session.Channel,
                Intent = session.Intent,
                PendingApprovals = session.PendingApprovals.ToList()
            };

            var names = new Dictionary<Guid, string?>();
            if (session.SelectedPersonIds.Count > 0 || session.PersonId != null)
            {
                var people = await _persons.GetAllPersons();
                foreach (var person in people.Where(p => p != null))
                    names[person!.PersonId] = person.Name;
            }
            if (session.PersonId != null && names.TryGetValue(session.PersonId.Value, out var single))
                state.SelectedPeople.Add(single ?? "contact");
            foreach (var id in session.SelectedPersonIds)
                state.SelectedPeople.Add(names.TryGetValue(id, out var name) ? name ?? "contact" : "contact");

            if (session.BatchId != null)
            {
                try
                {
                    var batch = await _outreach.GetAsync(session.BatchId.Value);
                    state.Intent = batch.Intent;
                    state.Channel = batch.Channel.ToString();
                }
                catch (KeyNotFoundException) { }
            }
            return state;
        }
    }
}
