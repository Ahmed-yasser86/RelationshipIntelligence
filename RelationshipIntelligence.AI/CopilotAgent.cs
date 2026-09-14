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
            Classify the user's relationship-assistant request. Respond with EXACTLY this JSON and nothing else:
            {"goal":"greet|info|explain|explore|plan|act|multistep|meeting|unknown",
             "personRefs":["names mentioned"],
             "wantsAction":true|false,
             "actionVerb":"log|create-event|create-goal|create-meeting|draft|approve|map|confirm|none",
             "isExplicitInstruction":true|false,
             "needsClarification":true|false,
             "clarificationPrompt":"..."|null,
             "switchDirection":true|false}
            Rules: greet for hellos and capability questions. meeting for anything about an upcoming or past meeting. multistep for batch/outreach flows with several people. act for single explicit do-this requests. switchDirection true when the message abandons the current task described below. isExplicitInstruction true only when the message itself orders a concrete mutation ("log a call with Salma"). needsClarification true only when required information is missing (e.g. meeting with nobody named, action on nobody).
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

            // A just-made disambiguation pick sets the focus; routing below
            // then skips name extraction because the focus is already resolved.
            AdoptPendingChoice(session, request.Message.Trim());

            // One-word confirmations for a proposed log execute immediately;
            // anything else that is not more detail releases the proposal so a
            // later "yes" can never record something abandoned.
            if (session.PendingLog != null)
            {
                var verdict = ClassifyLogReply(request.Message, session.PendingLog);
                if (verdict == LogReply.Confirm)
                {
                    var pending = session.PendingLog;
                    session.PendingLog = null;
                    var response0 = new AgentResponse { SessionId = session.SessionId };
                    try
                    {
                        var outcome = await _actions.LogInteractionAsync(
                            pending.PersonId.ToString(), pending.Type, pending.Title, true);
                        using var doc = JsonDocument.Parse(outcome);
                        if (doc.RootElement.TryGetProperty("error", out _))
                        {
                            response0.Text = "I couldn't record that — " +
                                (doc.RootElement.GetProperty("error").GetString() ?? "unknown validation problem.");
                        }
                        else
                        {
                            var who = await _persons.GetPersonByPersonId(pending.PersonId);
                            response0.Text = $"Logged a {pending.Type} with {who?.Name ?? "your contact"}: \"{pending.Title}\".";
                            response0.Actions.Add(new AgentAction { Kind = "open", Label = "Open relationship", Payload = pending.PersonId.ToString() });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Confirmed interaction log failed");
                        response0.Text = "I couldn't record that. Nothing was saved — tell me again and I'll retry.";
                    }
                    await _sessions.SaveAsync(ownerId, session);
                    response0.WorkingState = await WorkingStateAsync(session);
                    return response0;
                }
                if (verdict == LogReply.Discard)
                    session.PendingLog = null;
                else if (verdict == LogReply.Detail)
                {
                    var person = await _persons.GetPersonByPersonId(session.PendingLog.PersonId);
                    var updated = ProposeLog(session.PendingLog.PersonId, person?.Name ?? "contact", request.Message);
                    if (updated != null)
                        session.PendingLog = updated;
                }
                else
                    session.PendingLog = null;
            }

            var kernel = await _kernels.CreateAsync();
            kernel.Plugins.AddFromObject(_query, "relationships");
            kernel.Plugins.AddFromObject(_planning, "planning");
            kernel.Plugins.AddFromObject(_actions, "actions");

            // Deterministic pre-route: organization questions ("who works at
            // X") and typo'd names never reach the LLM's person tools, which
            // only do exact search. Handle them here with the right tool and
            // evidence, so the answer can never be "nobody" when members
            // exist, nor "I don't have" when it is a one-letter typo.
            var fast = await TryFastResolveAsync(session, request.Message.Trim());
            if (fast != null)
            {
                await _sessions.SaveAsync(ownerId, session);
                fast.WorkingState = await WorkingStateAsync(session);
                return fast;
            }

            var goal = await ClassifyAsync(kernel, session, request);
            // The classifier is advisory: greet/act/meeting/multistep route as
            // classified, but any other label with strong flow verbs still
            // reaches its flow instead of dissolving into chat.
            if (!string.Equals(goal.Goal, "greet", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(goal.Goal, "act", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(goal.Goal, "meeting", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(goal.Goal, "multistep", StringComparison.OrdinalIgnoreCase))
            {
                var fallback = FallbackGoal(request.Message, session.BatchId != null);
                if (!string.Equals(fallback, "unknown", StringComparison.OrdinalIgnoreCase))
                    goal.Goal = fallback;
            }
            // A lone batch verb ("approve", "continue") inside an open batch is
            // a refinement, not a cold action: never bounce it to confirmation.
            if (session.BatchId != null && string.Equals(goal.Goal, "act", StringComparison.OrdinalIgnoreCase) &&
                !goal.IsExplicitInstruction && IsBatchVerb(request.Message))
                goal.Goal = "multistep";
            var response = new AgentResponse { SessionId = session.SessionId };

            if (goal.SwitchDirection && session.CurrentTask != null)
            {
                response.Text += $"Switching from {DescribeTask(session)} to your new request. Anything not yet approved stays undone. ";
                session.CurrentTask = null;
                session.PendingApprovals.Clear();
                session.BatchId = null;
                session.SelectedPersonIds.Clear();
            }

            switch (goal.Goal)
            {
                case "greet":
                    response.Text += "Hi — I keep track of your relationships with you. Ask what is happening with someone, who needs attention, what is coming up, or tell me to prepare messages, a call brief, or a meeting.";
                    response.Actions.Add(new AgentAction { Kind = "ask", Label = "Who needs attention?" });
                    break;
                case "meeting":
                    await HandleMeetingAsync(kernel, session, request, goal, response);
                    break;
                case "multistep":
                    await HandleOutreachFlowAsync(kernel, session, request, goal, response);
                    break;
                case "act":
                    await HandleActionAsync(session, request, goal, response);
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

        private static string DescribeTask(AgentSession session) =>
            string.IsNullOrWhiteSpace(session.CurrentTask) ? "the previous task" : session.CurrentTask;

        private enum LogReply { Confirm, Discard, Detail, Other }

        private static LogReply ClassifyLogReply(string message, PendingLogProposal _)
        {
            var lower = message.ToLowerInvariant();
            if (lower.Contains("forget") || lower.Contains("no ") || lower == "no" ||
                lower.Contains("cancel") || lower.Contains("never mind") || lower.Contains("drop it"))
                return LogReply.Discard;
            if (System.Text.RegularExpressions.Regex.IsMatch(lower,
                @"\b(yes|yeah|yep|confirm|do it|record it|log it|ok|okay|sure|please do|go ahead)\b"))
                return LogReply.Confirm;
            if (lower.Contains("title") || lower.Contains("call") || lower.Contains("email") ||
                lower.Contains("meeting") || lower.Contains("message") || lower.Contains("text") ||
                lower.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).Length > 3)
                return LogReply.Detail;
            return LogReply.Other;
        }

        /// <summary>
        /// Builds a concrete log proposal from the user's own words: detects
        /// the type and shapes the rest into a title. Returns null when there
        /// is not enough to propose, in which case the agent asks once.
        /// </summary>
        private static PendingLogProposal? ProposeLog(Guid personId, string name, string message)
        {
            var lower = message.ToLowerInvariant();
            string? type = null;
            if (lower.Contains("call") || lower.Contains("phone") || lower.Contains("rang") ||
                lower.Contains("called") || lower.Contains("spoke") || lower.Contains("talked"))
                type = "Call";
            else if (lower.Contains("email") || lower.Contains("e-mail") || lower.Contains("mailed"))
                type = "Email";
            else if (lower.Contains("meeting") || lower.Contains("met with") || lower.Contains("sat down"))
                type = "Meeting";
            else if (lower.Contains("message") || lower.Contains("text") || lower.Contains("whatsapp") ||
                lower.Contains("sms") || lower.Contains("chatted") || lower.Contains("chat"))
                type = "Message";
            if (type == null)
                return null;

            // Cut the intent preamble through the person's name: everything
            // before it is instruction ("log an interaction for Salma"),
            // everything after is the content to record.
            var title = message.Trim();
            var nameIdx = title.IndexOf(name, StringComparison.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(name) && nameIdx >= 0)
                title = title[(nameIdx + name.Length)..];
            else
                title = System.Text.RegularExpressions.Regex.Replace(title,
                    @"^(please\s+)?(log|add|record)\s+(a\s+|an\s+|this\s+|that\s+)?", "",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            foreach (var filler in new[] { type, "about", "that", "for me", "with me", "please", "today's", "today", "yesterday" })
            {
                if (string.IsNullOrWhiteSpace(filler))
                    continue;
                title = System.Text.RegularExpressions.Regex.Replace(title,
                    @"\b" + System.Text.RegularExpressions.Regex.Escape(filler.Trim()) + @"\b", "",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }
            title = System.Text.RegularExpressions.Regex.Replace(title.Trim(), @"\s+", " ").Trim(' ', ',', '.', '!', '?', '"', '\'');
            title = System.Text.RegularExpressions.Regex.Replace(title, @"\s+(in|on|at|for|with|to|of)$", "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (title.Length == 0)
                return null;
            if (title.Length > 100)
                title = title[..100].Trim();
            return new PendingLogProposal { PersonId = personId, Type = type, Title = title };
        }

        private static string FallbackGoal(string message, bool batchActive = false)
        {
            var lower = message.ToLowerInvariant();
            if (lower.Contains("meeting") && (lower.Contains("prepare") || lower.Contains("tomorrow") || lower.Contains("plan")))
                return "meeting";
            if (lower.Contains("prepare") || lower.Contains("draft") || lower.Contains("outreach") ||
                lower.Contains("reconnect") || lower.Contains("follow up with") || lower.Contains("contact them") ||
                ((lower.Contains("message") || lower.Contains("email") || lower.Contains("batch")) &&
                 (lower.Contains("everyone") || lower.Contains("all") || lower.Contains("week") || lower.Contains("neglect"))))
                return "multistep";
            // Mid-flow refinements ("use LinkedIn", "approve", "continue")
            // belong to the open batch even when the classifier abstains.
            if (batchActive && (lower.Contains("linkedin") || lower.Contains("channel") ||
                lower.Contains("approve") || lower.Contains("continue") || lower.Contains("remove") ||
                lower.Contains("generate") || lower.Contains("settings")))
                return "multistep";
            return "unknown";
        }

        private static bool IsBatchVerb(string message)
        {
            var lower = message.ToLowerInvariant();
            return lower.Contains("approve") || lower.Contains("continue") || lower.Contains("generate") ||
                lower.Contains("prepare") || lower.Contains("channel") || lower.Contains("remove") ||
                lower.Contains("settings") || lower.Contains("draft") || lower.Contains("linkedin") ||
                lower.Contains("email") || lower.Contains("text message") || lower.Contains("call prep");
        }

        private sealed class GoalClassification
        {
            public string Goal { get; set; } = "unknown";
            public List<string> PersonRefs { get; set; } = new();
            public bool WantsAction { get; set; }
            public string ActionVerb { get; set; } = "none";
            public bool IsExplicitInstruction { get; set; }
            public bool NeedsClarification { get; set; }
            public string? ClarificationPrompt { get; set; }
            public bool SwitchDirection { get; set; }
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

        private async Task<Guid?> ResolvePersonAsync(string name)
        {
            var raw = await _query.SearchPeopleAsync(name);
            try
            {
                using var doc = JsonDocument.Parse(raw);
                if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() == 0)
                    return null;
                if (doc.RootElement.GetArrayLength() > 1)
                    return null;
                var id = doc.RootElement[0].GetProperty("personId").GetGuid();
                return id;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private async Task<List<(Guid Id, string Name)>> ResolveCandidatesAsync(string name)
        {
            return (await ResolveDetailedCandidatesAsync(name))
                .Select(c => (c.Id, c.Name)).ToList();
        }

        private async Task<List<(Guid Id, string Name, string? Org, string? Role)>> ResolveDetailedCandidatesAsync(string name)
        {
            var raw = await _query.SearchPeopleAsync(name);
            var result = new List<(Guid, string, string?, string?)>();
            try
            {
                using var doc = JsonDocument.Parse(raw);
                if (doc.RootElement.ValueKind != JsonValueKind.Array)
                    return result;
                foreach (var el in doc.RootElement.EnumerateArray().Take(5))
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

        private static readonly System.Text.RegularExpressions.Regex OrgQuestionPattern =
            new(@"\b(who|which)\b.{0,60}?\b(at|in|from|with)\b\s+(?<org>[A-Za-z][A-Za-z0-9 .&'-]{1,60})",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
                | System.Text.RegularExpressions.RegexOptions.Compiled);

        /// <summary>
        /// Deterministic pre-route for two question shapes the LLM path
        /// mishandles: (1) "who works at X" — exact-search tools answer
        /// "nobody" because X is an org, not a person; (2) typo'd names —
        /// the extractor takes the wrong token ("Smair") and search misses.
        /// Returns null when the message is neither shape, letting normal
        /// routing continue untouched.
        /// </summary>
        private async Task<AgentResponse?> TryFastResolveAsync(AgentSession session, string message)
        {
            if (string.IsNullOrWhiteSpace(message) || message.Length > 200)
                return null;

            var orgMatch = OrgQuestionPattern.Match(message);
            if (orgMatch.Success)
            {
                // Trailing clause ("List everyone", "show me all") is not part
                // of the company name: cut at the first sentence end or
                // command verb.
                var org = orgMatch.Groups["org"].Value;
                org = System.Text.RegularExpressions.Regex.Split(
                    org, @"[.?!]|\b(list|show|tell|give|display)\b",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase)[0].Trim();
                if (org.Length >= 2 && !PersonNameExtractor.DismissesPerson(message.ToLowerInvariant()))
                    return await AnswerOrganizationAsync(session, org);
            }

            // Typo path: scan every capitalized token (not just the first
            // non-initial one) for a near-match, so "Dina Smair" checks both
            // "Dina" (exact hit, unhelpful alone) and "Smair" (fuzzy hit).
            if (session.PersonId == null)
            {
                string? best = null;
                var bestScore = 0.0;
                foreach (System.Text.RegularExpressions.Match m in
                    System.Text.RegularExpressions.Regex.Matches(message, @"\b[A-Z][a-z]{2,}\b"))
                {
                    var token = m.Value;
                    if (await ResolveDetailedCandidatesAsync(token) is { Count: > 0 })
                        continue;
                    var near = await FindNearMatchesAsync(token);
                    if (near.Count == 0)
                        continue;
                    var score = PersonNameExtractor.Similarity(token, near[0].Name);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = token;
                    }
                }
                if (best != null)
                {
                    var near = await FindNearMatchesAsync(best);
                    var choices = PersonChoiceLabels.Build(near);
                    session.PendingCandidates.Clear();
                    session.PendingCandidates.AddRange(choices.Select(c =>
                        new AgentCandidateOption { PersonId = c.Id, Label = c.Label }));
                    return new AgentResponse
                    {
                        SessionId = session.SessionId,
                        Text = $"I don't have \"{best}\", but did you mean {string.Join(" / ", choices.Select(c => c.Label))}?",
                        NeedsInput = new AgentClarification { Prompt = "Did you mean one of these?", Options = choices.Select(c => c.Label).ToList() }
                    };
                }
            }
            return null;
        }

        private async Task<AgentResponse> AnswerOrganizationAsync(AgentSession session, string org)
        {
            var response = new AgentResponse { SessionId = session.SessionId };
            string raw;
            try
            {
                raw = await _query.ListOrganizationMembersAsync(org);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Organization lookup failed for {Org}", org);
                response.Text = $"I couldn't look up '{org}' right now — try the Organizations tab, or ask again in a moment.";
                return response;
            }
            try
            {
                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;
                if (root.TryGetProperty("count", out var countProp) && countProp.GetInt32() == 0)
                {
                    response.Text = $"Nobody in your network is listed at '{org}'. If someone there belongs in your contacts, add them and I'll include them next time.";
                    return response;
                }
                var members = root.GetProperty("members").EnumerateArray()
                    .Select(e => e.GetProperty("name").GetString() ?? "?").ToList();
                var count = root.TryGetProperty("count", out var c) ? c.GetInt32() : members.Count;
                var shown = string.Join(", ", members.Take(15));
                response.Text = count > members.Count
                    ? $"{count} people in your network work at {org}: {shown}, and {count - members.Count} more. Open the Organizations tab to see everyone."
                    : $"{count} {(count == 1 ? "person" : "people")} in your network {(count == 1 ? "works" : "work")} at {org}: {shown}.";
                foreach (var e in root.GetProperty("members").EnumerateArray())
                {
                    if (!e.TryGetProperty("personId", out var idProp))
                        continue;
                    var id = idProp.GetGuid();
                    var name = e.TryGetProperty("name", out var nProp) ? nProp.GetString() ?? "contact" : "contact";
                    response.Citations.Add(new CopilotCitation { Kind = "person", Id = id, Label = name });
                    response.Evidence.Add(new AgentEvidence
                    {
                        Title = name,
                        Detail = $"Listed at {org}.",
                        Kind = "observed",
                        RefId = id,
                        RefKind = "person"
                    });
                    response.Actions.Add(new AgentAction { Kind = "open", Label = $"Open {name}", Payload = id.ToString() });
                }
                return response;
            }
            catch (JsonException)
            {
                response.Text = $"I couldn't read the member list for '{org}' — try the Organizations tab to see everyone there.";
                return response;
            }
        }

        /// <summary>
        /// Typo-tolerant fallback over the user's own contacts. Scans the
        /// directory (bounded at 500) and keeps names scoring >= 0.55, top 3.
        /// Suggestions only: the caller presents them as "did you mean" and
        /// the user picks — nothing is ever auto-attached.
        /// </summary>
        private async Task<List<(Guid Id, string Name, string? Org, string? Role)>> FindNearMatchesAsync(string name)
        {
            var scored = new List<(Guid Id, string Name, string? Org, string? Role, double Score)>();
            try
            {
                var people = await _persons.GetAllPersons();
                foreach (var p in people.Where(p => p != null).Take(500))
                {
                    var score = PersonNameExtractor.Similarity(name, p!.Name ?? string.Empty);
                    if (score < 0.55)
                        continue;
                    var org = p!.Organizations.FirstOrDefault()?.Name;
                    var role = p!.CurrentRoles.FirstOrDefault()?.Role;
                    scored.Add((p!.PersonId, p!.Name ?? "?", org, role, score));
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Near-match scan skipped.");
                return new List<(Guid, string, string?, string?)>();
            }
            return scored
                .OrderByDescending(r => r.Score)
                .Take(3)
                .Select(r => (r.Id, r.Name, r.Org, r.Role))
                .ToList();
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
            if (session.PersonId == null)
            {
                var candidate = PersonNameExtractor.ExtractUnknownName(request.Message.Trim());
                if (candidate != null)
                {
                    var matches = await ResolveDetailedCandidatesAsync(candidate);
                    if (matches.Count == 0)
                    {
                        // Exact search found nothing: fall back to typo-tolerant
                        // suggestions ("Dina Smair" -> "Dina Samir?") instead of
                        // a dead-end. Suggestions only — never auto-attached.
                        var near = await FindNearMatchesAsync(candidate);
                        if (near.Count > 0)
                        {
                            var choices = PersonChoiceLabels.Build(near);
                            session.PendingCandidates.Clear();
                            session.PendingCandidates.AddRange(choices.Select(c =>
                                new AgentCandidateOption { PersonId = c.Id, Label = c.Label }));
                            response.Text += $"I don't have \"{candidate}\", but did you mean {string.Join(" / ", choices.Select(c => c.Label))}?";
                            response.NeedsInput = new AgentClarification { Prompt = "Did you mean one of these?", Options = choices.Select(c => c.Label).ToList() };
                            return;
                        }
                        response.Text += $"I don't have a contact matching \"{candidate}\". I can only reason about people in your network — want to add them first, or ask about someone else?";
                        return;
                    }
                    if (matches.Count > 1)
                    {
                        var choices = PersonChoiceLabels.Build(matches);
                        session.PendingCandidates.Clear();
                        session.PendingCandidates.AddRange(choices.Select(c =>
                            new AgentCandidateOption { PersonId = c.Id, Label = c.Label }));
                        response.Text += $"I found several people matching \"{candidate}\". Which one do you mean?";
                        response.NeedsInput = new AgentClarification { Prompt = "Which person?", Options = choices.Select(c => c.Label).ToList() };
                        return;
                    }
                    session.PersonId = matches[0].Id;
                }
            }
            if (goal.PersonRefs.Count > 0 && session.PersonId == null)
            {
                var candidates = await ResolveDetailedCandidatesAsync(goal.PersonRefs[0]);
                if (candidates.Count == 0)
                {
                    var near = await FindNearMatchesAsync(goal.PersonRefs[0]);
                    if (near.Count > 0)
                    {
                        var nearChoices = PersonChoiceLabels.Build(near);
                        session.PendingCandidates.Clear();
                        session.PendingCandidates.AddRange(nearChoices.Select(c =>
                            new AgentCandidateOption { PersonId = c.Id, Label = c.Label }));
                        response.Text += $"I don't have \"{goal.PersonRefs[0]}\", but did you mean {string.Join(" / ", nearChoices.Select(c => c.Label))}?";
                        response.NeedsInput = new AgentClarification { Prompt = "Did you mean one of these?", Options = nearChoices.Select(c => c.Label).ToList() };
                        return;
                    }
                    response.Text += $"I don't have a contact matching \"{goal.PersonRefs[0]}\". I can only reason about people in your network — want to add them first, or ask about someone else?";
                    return;
                }
                if (candidates.Count > 1)
                {
                    var choices = PersonChoiceLabels.Build(candidates);
                    session.PendingCandidates.Clear();
                    session.PendingCandidates.AddRange(choices.Select(c =>
                        new AgentCandidateOption { PersonId = c.Id, Label = c.Label }));
                    response.Text += $"I found several people matching \"{goal.PersonRefs[0]}\". Which one do you mean?";
                    response.NeedsInput = new AgentClarification
                    {
                        Prompt = "Which person?",
                        Options = choices.Select(c => c.Label).ToList()
                    };
                    session.PendingApprovals.Add("person:" + goal.PersonRefs[0]);
                    return;
                }
                session.PersonId = candidates[0].Id;
            }

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

        private async Task HandleActionAsync(AgentSession session, AgentChatRequest request, GoalClassification goal, AgentResponse response)
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
                var resolved = await ResolvePersonAsync(goal.PersonRefs[0]);
                if (resolved == null)
                {
                    var candidates = await ResolveCandidatesAsync(goal.PersonRefs[0]);
                    if (candidates.Count == 0)
                    {
                        response.Text += $"I don't have a contact matching \"{goal.PersonRefs[0]}\", so I did nothing.";
                        return;
                    }
                    response.Text += $"Several people match \"{goal.PersonRefs[0]}\". Which one?";
                    response.NeedsInput = new AgentClarification { Prompt = "Which person?", Options = candidates.Select(c => c.Name).ToList() };
                    return;
                }
                session.PersonId = resolved;
            }

            if (session.PersonId == null)
            {
                response.Text += "I need to know who this is about first.";
                response.NeedsInput = new AgentClarification { Prompt = "Which person?", Options = new List<string>() };
                return;
            }

            var person = await _persons.GetPersonByPersonId(session.PersonId);
            var name = person?.Name ?? "contact";
            switch (goal.ActionVerb)
            {
                case "log":
                    var proposal = ProposeLog(session.PersonId.Value, name, request.Message);
                    if (proposal != null)
                    {
                        session.PendingLog = proposal;
                        response.Text += $"I'll log a {proposal.Type} with {name} titled \"{proposal.Title}\". Say yes and I'll record it.";
                        response.NeedsInput = new AgentClarification
                        {
                            Prompt = "Record this interaction?",
                            Options = new List<string> { "Yes, log it", "No, forget it" }
                        };
                    }
                    else
                    {
                        response.Text += $"Ready to log an interaction with {name}. Tell me the type and what it was about — for example \"log a call about the proposal\" — and I'll record it.";
                    }
                    response.Actions.Add(new AgentAction { Kind = "log", Label = $"Log interaction with {name}", Payload = session.PersonId.ToString() });
                    break;
                case "create-event":
                    response.Text += $"What should I record for {name} — the occasion, the date, and whether it repeats yearly?";
                    session.PendingApprovals.Add("event:" + session.PersonId);
                    break;
                case "create-goal":
                    response.Text += $"What goal should I set for your relationship with {name}?";
                    session.PendingApprovals.Add("goal:" + session.PersonId);
                    break;
                default:
                    response.Text += $"Tell me exactly what to do for {name}, and I'll do it once you confirm.";
                    break;
            }
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

                var mapped = new List<string>();
                var missing = new List<string>();
                foreach (var name in goal.PersonRefs.Distinct(StringComparer.OrdinalIgnoreCase).Take(10))
                {
                    var candidates = await ResolveCandidatesAsync(name);
                    if (candidates.Count == 1) mapped.Add(name);
                    else missing.Add(name);
                }
                if (missing.Count > 0)
                {
                    response.Text += missing.Count == goal.PersonRefs.Distinct(StringComparer.OrdinalIgnoreCase).Count()
                        ? $"I couldn't match {string.Join(", ", missing)} to anyone in your network. Check the spelling, or add them as contacts first."
                        : $"I matched {string.Join(", ", mapped)} but not {string.Join(", ", missing)}. Who did you mean by {string.Join(", ", missing)}?";
                    if (missing.Count < goal.PersonRefs.Distinct(StringComparer.OrdinalIgnoreCase).Count())
                        response.NeedsInput = new AgentClarification { Prompt = "Who did you mean?", Options = new List<string>() };
                    session.CurrentTask = "meeting-prep";
                    return;
                }

                response.Text += $"I can prepare for a meeting with {string.Join(", ", mapped)}. Give me a title and date — for example \"Partnership review tomorrow at 10\" — and I'll create the preparation.";
                session.CurrentTask = "meeting-prep";
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
