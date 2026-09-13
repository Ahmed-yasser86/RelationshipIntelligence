using Servicess;
using ServiceContracts;
using ServiceContracts.DTOs.AgentDTOs;
using ServiceContracts.DTOs.CopilotDTOs;
using ServiceContracts.DTOs.MemoryDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RelationshipIntelligence.AI
{
    /// <summary>
    /// Deterministic rule-based implementation of the agentic loop, used when
    /// Copilot:Mode is Stub. Same stages as the live agent (understand,
    /// resolve, gather, clarify, act, confirm) driven by transparent rules
    /// over real services — no canned answers, no model calls.
    /// </summary>
    public sealed class StubCopilotAgent : ICopilotAgent
    {
        private readonly IAgentSessionStore _sessions;
        private readonly ICurrentUserService _currentUser;
        private readonly IRelationshipScoringService _scoring;
        private readonly IInteractionService _interactions;
        private readonly IRelationshipMemoryService _memory;
        private readonly IEventService _events;
        private readonly INetworkAnalysisService _network;
        private readonly IPersonSearcherService _searcher;
        private readonly IPersonGetterService _persons;
        private readonly IOutreachService _outreach;

        public StubCopilotAgent(
            IAgentSessionStore sessions,
            ICurrentUserService currentUser,
            IRelationshipScoringService scoring,
            IInteractionService interactions,
            IRelationshipMemoryService memory,
            IEventService events,
            INetworkAnalysisService network,
            IPersonSearcherService searcher,
            IPersonGetterService persons,
            IOutreachService outreach)
        {
            _sessions = sessions;
            _currentUser = currentUser;
            _scoring = scoring;
            _interactions = interactions;
            _memory = memory;
            _events = events;
            _network = network;
            _searcher = searcher;
            _persons = persons;
            _outreach = outreach;
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

            var response = new AgentResponse { SessionId = session.SessionId };
            var message = request.Message.Trim();
            var lower = message.ToLowerInvariant();

            if (IsGreeting(lower))
            {
                response.Text = "Hi — I keep track of your relationships with you. Ask what is happening with someone, who needs attention, what is coming up, or tell me to prepare messages, a call brief, or a meeting.";
                response.Actions.Add(new AgentAction { Kind = "open-queue", Label = "Who needs attention?" });
                await _sessions.SaveAsync(ownerId, session);
                response.WorkingState = await WorkingStateAsync(session);
                return response;
            }

            if (await AdoptPendingChoiceAsync(session, ownerId, message, lower, response))
                return response;

            var names = await KnownNamesAsync();
            var mentioned = names.Where(n => lower.Contains(n.ToLowerInvariant())).ToList();
            // Identical names collapse: two "Omar Khalil" contacts are one
            // ambiguous reference, not two distinct mentions.
            var distinctMentioned = mentioned.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (distinctMentioned.Count > 1)
            {
                if (IsComparison(lower))
                {
                    await AnswerCompareAsync(distinctMentioned.Take(3).ToList(), response);
                    await _sessions.SaveAsync(ownerId, session);
                    response.WorkingState = await WorkingStateAsync(session);
                    return response;
                }
                response.Text = $"Several names came up ({string.Join(", ", distinctMentioned)}). Which relationship should I focus on?";
                response.NeedsInput = new AgentClarification { Prompt = "Which person?", Options = distinctMentioned };
                await _sessions.SaveAsync(ownerId, session);
                response.WorkingState = await WorkingStateAsync(session);
                return response;
            }
            mentioned = distinctMentioned;
            if (mentioned.Count == 1)
            {
                var resolved = await ResolveAsync(mentioned[0]);
                if (resolved.Count == 1)
                {
                    session.PersonId = resolved[0].Id;
                    session.CurrentTask = null;
                }
                else if (resolved.Count > 1)
                {
                    var detailed = await ResolveDetailedAsync(mentioned[0]);
                    var choices = PersonChoiceLabels.Build(detailed);
                    session.PendingCandidates.Clear();
                    session.PendingCandidates.AddRange(choices.Select(c =>
                        new ServiceContracts.DTOs.AgentDTOs.AgentCandidateOption { PersonId = c.Id, Label = c.Label }));
                    response.Text = $"I found several people matching \"{mentioned[0]}\". Which one do you mean?";
                    response.NeedsInput = new AgentClarification { Prompt = "Which person?", Options = choices.Select(c => c.Label).ToList() };
                    await _sessions.SaveAsync(ownerId, session);
                    response.WorkingState = await WorkingStateAsync(session);
                    return response;
                }
            }

            if (IsMeetingPrep(lower) && !IsBatchRefinement(lower, session))
            {
                await HandleMeetingPrepAsync(session, mentioned, response);
                await _sessions.SaveAsync(ownerId, session);
                response.WorkingState = await WorkingStateAsync(session);
                return response;
            }

            if (IsBatchRequest(lower))
            {
                if (session.BatchId != null && !IsNewSearch(lower))
                {
                    await HandleOutreachFlowAsync(session, message, lower, response);
                    await _sessions.SaveAsync(ownerId, session);
                    response.WorkingState = await WorkingStateAsync(session);
                    return response;
                }
                session.BatchId = null;
                session.SelectedPersonIds.Clear();
                session.PendingApprovals.Clear();
                await HandleOutreachFlowAsync(session, message, lower, response);
                await _sessions.SaveAsync(ownerId, session);
                response.WorkingState = await WorkingStateAsync(session);
                return response;
            }

            string? unknownName = null;
            if (session.PersonId == null)
            {
                unknownName = PersonNameExtractor.ExtractUnknownName(message);
            }
            else
            {
                var current = await _persons.GetPersonByPersonId(session.PersonId);
                var currentName = (current?.Name ?? string.Empty).ToLowerInvariant();
                var other = PersonNameExtractor.ExtractUnknownName(message);
                if (other != null && !currentName.Contains(other.ToLowerInvariant()))
                    unknownName = other;
            }
            if (unknownName == null && PersonNameExtractor.DismissesPerson(lower))
            {
                session.PersonId = null;
                session.CurrentTask = null;
            }
            if (unknownName != null)
            {
                var resolved = await ResolveAsync(unknownName);
                if (resolved.Count == 0)
                {
                    response.Text = $"I don't have a contact matching \"{unknownName}\". I can only reason about people in your network — want to add them first, or ask about someone else?";
                    await _sessions.SaveAsync(ownerId, session);
                    response.WorkingState = await WorkingStateAsync(session);
                    return response;
                }
                if (resolved.Count > 1)
                {
                    var detailed = await ResolveDetailedAsync(unknownName);
                    var choices = PersonChoiceLabels.Build(detailed);
                    session.PendingCandidates.Clear();
                    session.PendingCandidates.AddRange(choices.Select(c =>
                        new ServiceContracts.DTOs.AgentDTOs.AgentCandidateOption { PersonId = c.Id, Label = c.Label }));
                    response.Text = $"I found several people matching \"{unknownName}\". Which one do you mean?";
                    response.NeedsInput = new AgentClarification { Prompt = "Which person?", Options = choices.Select(c => c.Label).ToList() };
                    await _sessions.SaveAsync(ownerId, session);
                    response.WorkingState = await WorkingStateAsync(session);
                    return response;
                }
                session.PersonId = resolved[0].Id;
            }

            if (IsFollowUp(lower) && session.PersonId != null)
            {
                await AnswerPersonAsync(session, session.PersonId.Value, lower, response);
                await _sessions.SaveAsync(ownerId, session);
                response.WorkingState = await WorkingStateAsync(session);
                return response;
            }

            if (session.PersonId != null && (IsFollowUp(lower) || IsGenericQuestion(lower) ||
                (HasPronounReference(lower) && !IsNewSearch(lower))))
            {
                await AnswerPersonAsync(session, session.PersonId.Value, lower, response);
                await _sessions.SaveAsync(ownerId, session);
                response.WorkingState = await WorkingStateAsync(session);
                return response;
            }

            if (session.BatchId != null && IsFollowUp(lower))
            {
                await AnswerBatchEvidenceAsync(session, response);
                await _sessions.SaveAsync(ownerId, session);
                response.WorkingState = await WorkingStateAsync(session);
                return response;
            }

            if (IsPromiseQuestion(lower))
            {
                await AnswerCommitmentsAsync(response);
                await _sessions.SaveAsync(ownerId, session);
                response.WorkingState = await WorkingStateAsync(session);
                return response;
            }

            if (IsDatesQuestion(lower))
            {
                await AnswerEventsAsync(response);
                await _sessions.SaveAsync(ownerId, session);
                response.WorkingState = await WorkingStateAsync(session);
                return response;
            }

            if (IsAttentionQuestion(lower))
            {
                await AnswerAttentionAsync(response);
                await _sessions.SaveAsync(ownerId, session);
                response.WorkingState = await WorkingStateAsync(session);
                return response;
            }

            if (IsChangeQuestion(lower))
            {
                await AnswerChangesAsync(response);
                await _sessions.SaveAsync(ownerId, session);
                response.WorkingState = await WorkingStateAsync(session);
                return response;
            }

            if (IsOverviewQuestion(lower))
            {
                await AnswerOverviewAsync(response);
                await _sessions.SaveAsync(ownerId, session);
                response.WorkingState = await WorkingStateAsync(session);
                return response;
            }

            if (HasPronounReference(lower))
            {
                if (session.PersonId != null)
                {
                    await AnswerPersonAsync(session, session.PersonId.Value, lower, response);
                    await _sessions.SaveAsync(ownerId, session);
                    response.WorkingState = await WorkingStateAsync(session);
                    return response;
                }
                response.Text = "Which person do you mean? Name someone in your network and I'll look into their relationship state, history, and what I'd do next.";
                response.Actions.Add(new AgentAction { Kind = "open-queue", Label = "Who needs attention?" });
                await _sessions.SaveAsync(ownerId, session);
                response.WorkingState = await WorkingStateAsync(session);
                return response;
            }

            response.Text = "I can help with that from your relationship data. Try asking who needs attention, what is coming up, what happened with someone by name, or tell me to prepare messages or a meeting. What would you like to do?";
            response.Actions.Add(new AgentAction { Kind = "open-queue", Label = "Who needs attention?" });
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

        private static bool IsGreeting(string lower) =>
            lower is "hi" or "hello" or "hey" || lower.StartsWith("hi ") || lower.StartsWith("hello ") ||
            lower.Contains("what can you") || lower.Contains("help me with") || lower.Contains("how do you work");

        private static bool IsFollowUp(string lower) =>
            lower.StartsWith("why ") || lower.StartsWith("why") || lower.StartsWith("what should i do") || lower.StartsWith("draft ") ||
            lower.Contains("that matter") || lower.Contains("about it") || lower == "why?" || lower.Contains("explain") ||
            lower.Contains("should i do") || lower.Contains("should do") || lower.Contains("recommend") ||
            lower.Contains("advice") || lower.Contains("suggest") || lower.Contains("what would you do") ||
            lower.Contains("next step") || lower.Contains("priorit") || lower.Contains("compar") ||
            lower.Contains("evidence") || lower.Contains("support") || lower.Contains("based on");

        private static bool IsGenericQuestion(string lower) =>
            lower.Contains("what happened") || lower.Contains("tell me about") || lower.Contains("going on") ||
            lower.Contains("last talk") || lower.Contains("last spoke") || lower.Contains("changed");

        private static bool IsBatchRequest(string lower) =>
            lower.Contains("prepare") || lower.Contains("draft") || lower.Contains("build") ||
            lower.Contains("outreach") || lower.Contains("message") || lower.Contains("email") ||
            lower.Contains("linkedin") || lower.Contains("reconnect") || lower.Contains("batch") ||
            lower.Contains("approve") || lower.Contains("reach out") || lower.Contains("contact them") ||
            lower.Contains("follow up with") ||
            (lower.Contains("call") && (lower.Contains("prepare") || lower.Contains("brief") || lower.Contains("for ")));

        private static bool IsNewSearch(string lower) =>
            lower.Contains("find") || lower.Contains("who should") || lower.Contains("show me") ||
            lower.Contains("everyone") || lower.Contains("attention queue");

        private static bool IsOutreachFlow(string lower) => IsBatchRequest(lower);

        private static bool IsBatchRefinement(string lower, AgentSession session) =>
            session.BatchId != null && !IsNewSearch(lower);

        private static bool IsMeetingPrep(string lower) =>
            lower.Contains("meeting") && (lower.Contains("prepare") || lower.Contains("tomorrow") || lower.Contains("what should i know"));

        private static bool IsPromiseQuestion(string lower) =>
            lower.Contains("promise") || lower.Contains("promised") || lower.Contains("owe ");

        private static bool IsDatesQuestion(string lower) =>
            lower.Contains("birthday") || lower.Contains("date") || lower.Contains("event") ||
            lower.Contains("occasion") || lower.Contains("coming up") || lower.Contains("anniversary");

        private static bool IsAttentionQuestion(string lower) =>
            lower.Contains("attention") || lower.Contains("follow up with") || lower.Contains("neglect") ||
            lower.Contains("losing touch") || lower.Contains("drift") || lower.Contains("who should i contact") ||
            lower.Contains("contact this week");

        private static bool IsChangeQuestion(string lower) =>
            lower.Contains("changed") || lower.Contains("changing") || lower.Contains("healthier") ||
            lower.Contains("recently") && lower.Contains("relationship");

        // Broad network evaluations: aggregate first, then go deeper
        // only where attention is actually needed — never a canned dump.
        private static bool IsOverviewQuestion(string lower) =>
            lower.Contains("all my contacts") || lower.Contains("all contacts") ||
            lower.Contains("general evaluation") || lower.Contains("overview of") ||
            lower.Contains("brief about all") || lower.Contains("everyone") ||
            lower.Contains("whole network") || lower.Contains("my relationships") ||
            lower.Contains("overestimating") || lower.Contains("neglecting");

        // Side-by-side asks: compare, prioritize, choose between named people.
        private static bool IsComparison(string lower) =>
            lower.Contains("compar") || lower.Contains("priorit") || lower.Contains(" versus ") ||
            lower.Contains(" vs ") || lower.Contains(" vs. ") || lower.Contains("between") ||
            lower.Contains("both") || lower.Contains("who should");

        // Pronoun follow-ups: "him/her/them" inherit the session focus.
        private static bool HasPronounReference(string lower) =>
            System.Text.RegularExpressions.Regex.IsMatch(lower,
                @"\b(him|her|them|his|hers|their|theirs|he|she|they)\b");

        private async Task<List<string>> KnownNamesAsync()
        {
            var people = await _persons.GetAllPersons();
            return people.Where(p => p != null && !string.IsNullOrWhiteSpace(p.Name)).Select(p => p!.Name!).ToList();
        }

        private async Task<List<(Guid Id, string Name)>> ResolveAsync(string name)
        {
            var result = await _searcher.SearchPersonsBy_Batched(name, "Name", 1, 10);
            return result.Items.Select(p => (p.PersonId, p.Name ?? "?")).ToList();
        }

        private async Task<List<(Guid Id, string Name, string? Org, string? Role)>> ResolveDetailedAsync(string name)
        {
            var result = await _searcher.SearchPersonsBy_Batched(name, "Name", 1, 10);
            return result.Items.Select(p => (
                p.PersonId,
                p.Name ?? "?",
                Org: p.Circles.FirstOrDefault()?.Name,
                Role: p.ContactItemRoles.FirstOrDefault()?.Role)).ToList();
        }

        /// <summary>
        /// Applies a pending disambiguation pick. Returns true when the message
        /// chose one of the offered labels (exact, case-insensitive) and answers
        /// for that person immediately. Anything else clears the pending pick so
        /// a new question is never trapped behind an old clarification.
        /// </summary>
        private async Task<bool> AdoptPendingChoiceAsync(
            AgentSession session, Guid ownerId, string message, string lower, AgentResponse response)
        {
            if (session.PendingCandidates.Count == 0)
                return false;
            var options = session.PendingCandidates.Select(c => (c.PersonId, c.Label)).ToList();
            var picked = PersonChoiceLabels.MatchChoice(options, message);
            session.PendingCandidates.Clear();
            if (picked == null)
                return false;
            session.PersonId = picked.Value;
            session.CurrentTask = null;
            await AnswerPersonAsync(session, picked.Value, lower, response);
            await _sessions.SaveAsync(ownerId, session);
            response.WorkingState = await WorkingStateAsync(session);
            return true;
        }

        private async Task AnswerPersonAsync(AgentSession session, Guid personId, string lower, AgentResponse response)
        {
            var person = await _persons.GetPersonByPersonId(personId);
            if (person == null)
            {
                response.Text = "I don't have a contact with that id.";
                return;
            }

            response.Activity.Add($"Checking {person.Name ?? "their"} relationship state…");
            var queue = await _scoring.GetQueueAsync(200);
            var state = queue.FirstOrDefault(q => q.PersonId == personId);
            response.Activity.Add("Looking at the relationship history…");
            var interactions = await _interactions.ListForPersonAsync(personId);
            var last = interactions.OrderByDescending(i => i.TimeOfInteraction).FirstOrDefault();
            var memory = await _memory.ListForPersonAsync(personId);
            var active = memory.Where(m => m.Status == 0).ToList();
            var commitments = active.Where(m => m.Kind == Entities.RelationshipMemoryKind.Commitment).ToList();
            var displayName = person.Name ?? "This contact";
            response.Activity.Add("I found enough context.");

            var sb = new StringBuilder();
            if (state != null)
                sb.Append($"{displayName} sits at {state.Band.ToLowerInvariant()} with urgency {Math.Round(state.UrgencyScore)} out of 100. ");
            if (last == null)
                sb.Append($"{displayName} has no logged contact yet, so there is no rhythm to read. ");
            else
            {
                var days = TieDecayModel.SilenceDays(last.TimeOfInteraction, DateTime.UtcNow);
                sb.Append($"{displayName} was last in touch {days}d ago ({last.InteractionTitle}). ");
            }
            if (lower.Contains("promise") || lower.Contains("commit"))
            {
                sb.Append(commitments.Count == 0
                    ? $"No open commitments are recorded for {displayName}."
                    : $"Open commitments: {string.Join("; ", commitments.Take(3).Select(c => c.Title))}.");
            }
            else if (lower.Contains("why") || lower.Contains("matter") || lower.Contains("queue") ||
                lower.Contains("attention") || lower.Contains("urgent") || lower.Contains("need"))
            {
                // Attention verdict for the person in focus.
                if (state == null)
                    sb.Append("They are not currently in the attention queue.");
                else if (state.UrgencyScore > 65)
                    sb.Append($"Yes — {displayName} needs attention: {state.Band.ToLowerInvariant()} at urgency {Math.Round(state.UrgencyScore)}. " +
                        (last == null ? "No contact is recorded yet."
                        : $"Quiet {TieDecayModel.SilenceDays(last.TimeOfInteraction, DateTime.UtcNow)}d" +
                        (state.CadenceReferenceDays == null ? "." : $" against a ~{Math.Round(state.CadenceReferenceDays.Value)}d rhythm.")));
                else
                    sb.Append($"No — {displayName} looks steady: {state.Band.ToLowerInvariant()} at urgency {Math.Round(state.UrgencyScore)}. They surface lower in the queue than more drifted relationships.");
            }
            else if (lower.Contains("should i do") || lower.Contains("should do") || lower.Contains("draft") ||
                lower.Contains("recommend") || lower.Contains("advice") || lower.Contains("suggest") ||
                lower.Contains("what would you do") || lower.Contains("next step") || lower.Contains("priorit"))
            {
                // Recommendation from actual data: open commitment first,
                // else evidence-shaped guidance — never a generic platitude.
                if (commitments.Count > 0)
                    sb.Append($"Start from the open commitment: {commitments[0].Title}. I can draft a message or prepare a call brief — say the word.");
                else if (state == null || last == null)
                    sb.Append("There is no scored history yet — log the latest contact first so the rhythm stays honest, or tell me to draft a check-in message.");
                else if (state.UrgencyScore > 65)
                    sb.Append($"I'd send a short follow-up rather than wait longer — quiet {TieDecayModel.SilenceDays(last.TimeOfInteraction, DateTime.UtcNow)}d against a ~{(state.CadenceReferenceDays == null ? "?" : Math.Round(state.CadenceReferenceDays.Value).ToString())}d rhythm. I can draft it — say the word.");
                else
                    sb.Append("Nothing looks overdue — their score is modest. I can still draft a light check-in if you want.");
            }
            else
            {
                if (commitments.Count > 0)
                    sb.Append($"Open commitments: {string.Join("; ", commitments.Take(3).Select(c => c.Title))}. ");
                else if (active.Count > 0)
                    sb.Append($"Recorded context: {string.Join("; ", active.Take(3).Select(m => m.Title))}. ");
            }

            response.Text = sb.ToString().Trim();
            response.Citations.Add(new CopilotCitation { Kind = "person", Id = personId, Label = person.Name ?? "contact" });
            response.Evidence.Add(new AgentEvidence
            {
                Title = $"{displayName} — basis",
                Detail = state == null
                    ? $"{interactions.Count} logged interaction(s), no scored state."
                    : $"{interactions.Count} interaction(s); band {state.Band}, urgency {Math.Round(state.UrgencyScore)}; last contact {(last == null ? "never" : last.TimeOfInteraction.ToString("yyyy-MM-dd"))}.",
                Kind = "observed",
                RefId = personId,
                RefKind = "person"
            });
            response.Actions.Add(new AgentAction { Kind = "open", Label = $"Open {displayName}", Payload = personId.ToString() });
            response.Actions.Add(new AgentAction { Kind = "log", Label = "Log interaction", Payload = personId.ToString() });
            response.Actions.Add(new AgentAction { Kind = "explain", Label = "Explain score", Payload = personId.ToString() });
        }

        private async Task AnswerCommitmentsAsync(AgentResponse response)
        {
            var people = await _persons.GetAllPersons();
            var found = new List<(string Person, string Title)>();
            foreach (var person in people.Where(p => p != null).Take(200))
            {
                List<MemoryEntryResponse> entries;
                try
                {
                    entries = await _memory.ListForPersonAsync(person!.PersonId);
                }
                catch (KeyNotFoundException) { continue; }
                foreach (var entry in entries.Where(e => e.Status == 0 && e.Kind == Entities.RelationshipMemoryKind.Commitment).Take(3))
                    found.Add((person!.Name ?? "contact", entry.Title));
                if (found.Count >= 8) break;
            }

            if (found.Count == 0)
            {
                response.Text = "No open commitments are recorded for anyone. When you promise something, record it as a commitment and I will surface it here.";
                return;
            }
            response.Text = "These promises are still open: " +
                string.Join("; ", found.Take(5).Select(f => $"{f.Person} — {f.Title}")) +
                (found.Count > 5 ? $", and {found.Count - 5} more." : ".") +
                " Tell me which one to act on.";
            foreach (var (person, title) in found.Take(5))
                response.Evidence.Add(new AgentEvidence { Title = person, Detail = title, Kind = "observed" });
        }

        private async Task AnswerEventsAsync(AgentResponse response)
        {
            var upcoming = await _events.GetUpcomingAsync(30);
            if (upcoming.Count == 0)
            {
                response.Text = "No dates are recorded in the next 30 days. Add birthdays and milestones on each person's page and I will watch them with the relationship state.";
                return;
            }
            var queue = await _scoring.GetQueueAsync(200);
            var states = queue.ToDictionary(q => q.PersonId);
            var parts = new List<string>();
            foreach (var e in upcoming.Take(6))
            {
                var line = $"{e.PersonName ?? "Someone"} — {e.Title} {(e.InDays == 0 ? "today" : $"in {e.InDays}d")}";
                if (states.TryGetValue(e.PersonId, out var s) && s.LastContactAtUtc != null)
                {
                    var silent = s.SilenceDays ?? TieDecayModel.SilenceDays(s.LastContactAtUtc, DateTime.UtcNow);
                    if (silent > 21) line += $" (quiet for {silent}d)";
                }
                parts.Add(line);
            }
            response.Text = "Coming up: " + string.Join("; ", parts) + ".";
            foreach (var e in upcoming.Take(6))
                response.Evidence.Add(new AgentEvidence { Title = e.Title, Detail = $"{e.PersonName} in {e.InDays}d", Kind = "observed", RefId = e.PersonId, RefKind = "person" });
        }

        /// <summary>
        /// Broad network evaluation: aggregate the full queue into
        /// band patterns, then name only the relationships that actually need
        /// attention — progressive retrieval, not a data dump.
        /// </summary>
        private async Task AnswerOverviewAsync(AgentResponse response)
        {
            response.Activity.Add("Reading your relationship queue…");
            var queue = await _scoring.GetQueueAsync(200);
            response.Activity.Add($"Reviewed {queue.Count} ranked relationship(s).");
            if (queue.Count == 0)
            {
                response.Text = "Every relationship is within its natural rhythm, and nothing is recorded as needing attention. Log interactions and I will start reading patterns.";
                return;
            }
            var critical = queue.Count(q => q.Band == "Critical");
            var atRisk = queue.Count(q => q.Band == "AtRisk");
            var drifting = queue.Count(q => q.Band == "Drifting");
            var sb = new StringBuilder();
            sb.Append($"Across {queue.Count} ranked relationship(s): {critical} critical, {atRisk} at risk, {drifting} drifting. ");
            var top = queue.Take(3).ToList();
            response.Activity.Add("Checking what the top relationships need…");
            sb.Append("Worth your attention first: ");
            sb.Append(string.Join("; ", top.Select(q =>
                $"{q.Name} ({q.Band.ToLowerInvariant()}, urgency {Math.Round(q.UrgencyScore)})" +
                (q.LastContactAtUtc == null ? " — no contact recorded yet"
                : $" — quiet {(q.SilenceDays ?? TieDecayModel.SilenceDays(q.LastContactAtUtc, DateTime.UtcNow))}d"))));
            sb.Append(". Name anyone and I'll investigate their history and recommend a next step.");
            response.Text = sb.ToString();
            foreach (var q in top)
            {
                response.Citations.Add(new CopilotCitation { Kind = "person", Id = q.PersonId, Label = q.Name });
                response.Evidence.Add(new AgentEvidence
                {
                    Title = q.Name,
                    Detail = $"Band {q.Band}, urgency {Math.Round(q.UrgencyScore)}, {q.InteractionCount} interactions.",
                    Kind = "derived",
                    RefId = q.PersonId,
                    RefKind = "person"
                });
            }
            response.Activity.Add("I found enough context.");
            response.Actions.Add(new AgentAction { Kind = "open-queue", Label = "Open attention queue" });
        }

        /// <summary>
        /// Side-by-side comparison from canonical state: retrieve each named
        /// person, compare band, urgency, silence, and open commitments, then
        /// explain who to prioritize and why.
        /// </summary>
        private async Task AnswerCompareAsync(List<string> mentioned, AgentResponse response)
        {
            response.Activity.Add("Reading each relationship state…");
            var queue = await _scoring.GetQueueAsync(200);
            var rows = new List<(string Name, ServiceContracts.DTOs.RelationshipHealthResponse? State, int Silence, int Commitments)>();
            foreach (var name in mentioned)
            {
                var resolved = await ResolveAsync(name);
                if (resolved.Count == 0)
                    continue;
                var id = resolved[0].Id;
                var state = queue.FirstOrDefault(q => q.PersonId == id);
                var memory = await _memory.ListForPersonAsync(id);
                var open = memory.Count(m => m.Status == 0 && m.Kind == Entities.RelationshipMemoryKind.Commitment);
                var silent = state?.SilenceDays
                    ?? (state?.LastContactAtUtc == null ? (int?)null : TieDecayModel.SilenceDays(state.LastContactAtUtc, DateTime.UtcNow));
                rows.Add((resolved[0].Name, state, silent ?? -1, open));
            }
            if (rows.Count < 2)
            {
                response.Text = "I could only place one of those names in your network — name two contacts I know and I'll compare them.";
                return;
            }
            response.Activity.Add("Comparing urgency, silence, and open commitments…");
            var ordered = rows.OrderByDescending(r => r.State?.UrgencyScore ?? -1).ToList();
            var sb = new StringBuilder();
            sb.Append(string.Join(" — ", ordered.Select(r =>
                $"{r.Name}: {(r.State == null ? "unscored" : $"{r.State.Band.ToLowerInvariant()}, urgency {Math.Round(r.State.UrgencyScore)}")}" +
                (r.Silence < 0 ? "" : $", quiet {r.Silence}d") +
                (r.Commitments == 0 ? "" : $", {r.Commitments} open commitment(s)"))));
            var top = ordered[0];
            sb.Append($". I'd prioritize {top.Name}: " +
                (top.Commitments > 0 ? "an open commitment is waiting. "
                : top.State == null ? "they have no scored history yet, so a first touch establishes the rhythm. "
                : $"highest urgency at {Math.Round(top.State.UrgencyScore)}. "));
            sb.Append("Say the word and I'll draft the message.");
            response.Text = sb.ToString();
            foreach (var r in ordered)
            {
                var id = queue.FirstOrDefault(q => q.Name == r.Name)?.PersonId;
                if (id != null)
                {
                    response.Citations.Add(new CopilotCitation { Kind = "person", Id = id, Label = r.Name });
                    response.Evidence.Add(new AgentEvidence
                    {
                        Title = r.Name,
                        Detail = r.State == null ? "Unscored — no ranked state."
                            : $"Band {r.State.Band}, urgency {Math.Round(r.State.UrgencyScore)}, {r.State.InteractionCount} interactions.",
                        Kind = "derived",
                        RefId = id,
                        RefKind = "person"
                    });
                }
            }
            response.Activity.Add("I found enough context.");
        }

        /// <summary>
        /// Evidence behind the current shortlist: each member with the reason
        /// they were selected — the same reasons shown in the outreach flow.
        /// </summary>
        private async Task AnswerBatchEvidenceAsync(AgentSession session, AgentResponse response)
        {
            response.Activity.Add("Recalling why each person was shortlisted…");
            try
            {
                var batch = await _outreach.GetAsync(session.BatchId!.Value);
                var members = batch.Members.Where(m => !m.Excluded).Take(5).ToList();
                if (members.Count == 0)
                {
                    response.Text = "The shortlist is currently empty — nobody is selected, so there is no evidence to show.";
                    return;
                }
                response.Text = "The shortlist rests on: " + string.Join("; ",
                    members.Select(m => $"{m.PersonName ?? "A contact"} — {m.Reason}")) + ".";
                foreach (var m in members)
                {
                    response.Citations.Add(new CopilotCitation { Kind = "person", Id = m.PersonId, Label = m.PersonName ?? "contact" });
                    response.Evidence.Add(new AgentEvidence
                    {
                        Title = m.PersonName ?? "contact",
                        Detail = m.Reason,
                        Kind = "observed",
                        RefId = m.PersonId,
                        RefKind = "person"
                    });
                }
                response.Activity.Add("I found enough context.");
            }
            catch (Exception)
            {
                response.Text = "I could not reopen that shortlist — it may have been discarded. Tell me who to look at and I'll start over.";
            }
        }

        private async Task AnswerAttentionAsync(AgentResponse response)
        {
            response.Activity.Add("Reading your attention queue…");
            var queue = await _scoring.GetQueueAsync(7);
            if (queue.Count == 0)
            {
                response.Text = "Every relationship is within its natural rhythm. Nothing is asking for action right now.";
                return;
            }
            response.Text = "Deserving attention now: " + string.Join("; ", queue.Take(4).Select(q =>
                $"{q.Name} is {q.Band.ToLowerInvariant()} (urgency {Math.Round(q.UrgencyScore)})" +
                (q.LastContactAtUtc == null ? " with no contact recorded" :
                $", last contact {(q.SilenceDays ?? TieDecayModel.SilenceDays(q.LastContactAtUtc, DateTime.UtcNow))}d ago" +
                (q.CadenceReferenceDays == null ? "" : $" against a ~{Math.Round(q.CadenceReferenceDays.Value)}d rhythm")))) + ".";
            foreach (var q in queue.Take(4))
            {
                response.Citations.Add(new CopilotCitation { Kind = "person", Id = q.PersonId, Label = q.Name });
                response.Evidence.Add(new AgentEvidence
                {
                    Title = q.Name,
                    Detail = $"Band {q.Band}, urgency {Math.Round(q.UrgencyScore)}, {q.InteractionCount} interactions.",
                    Kind = "derived",
                    RefId = q.PersonId,
                    RefKind = "person"
                });
            }
            response.Actions.Add(new AgentAction { Kind = "open-queue", Label = "Open attention queue" });
        }

        private async Task AnswerChangesAsync(AgentResponse response)
        {
            var queue = await _scoring.GetQueueAsync(50);
            var movers = queue.Where(q => q.SilenceQuantile != null).OrderByDescending(q => q.UrgencyScore).Take(4).ToList();
            if (movers.Count == 0)
            {
                response.Text = "No measurable movement — histories are too thin to compare yet. Log interactions and the trajectory will appear.";
                return;
            }
            response.Text = "Most changed lately: " + string.Join("; ", movers.Select(q =>
                $"{q.Name} ({q.Band}, current silence longer than {Math.Round((q.SilenceQuantile ?? 0) * 100)}% of past gaps)")) + ".";
        }

        private async Task HandleOutreachFlowAsync(AgentSession session, string message, string lower, AgentResponse response)
        {
            if (session.BatchId == null)
            {
                var parsed = OutreachIntentMatcher.Match(message);
                if (parsed.NeedsClarification)
                {
                    response.Text = parsed.ClarificationPrompt ?? "Tell me who you want to reach.";
                    response.NeedsInput = new AgentClarification { Prompt = "Who should be included?", Options = new List<string>() };
                    session.CurrentTask = "outreach";
                    return;
                }
                var batch = await _outreach.BuildFromSignalsAsync(new ServiceContracts.DTOs.OutreachDTOs.BuildBatchFromSignalsRequest
                {
                    SignalFilters = parsed.SignalFilters,
                    TimeWindowDays = parsed.TimeWindowDays,
                    MaxMembers = 12,
                    Intent = string.IsNullOrWhiteSpace(parsed.IntentText) ? "Reconnect" : parsed.IntentText
                });
                session.BatchId = batch.OutreachBatchId;
                session.CurrentTask = "outreach";
                session.SelectedPersonIds.Clear();
                session.SelectedPersonIds.AddRange(batch.Members.Where(m => !m.Excluded).Select(m => m.PersonId));
                if (parsed.Channel != null)
                {
                    await _outreach.UpdateSettingsAsync(batch.OutreachBatchId, new ServiceContracts.DTOs.OutreachDTOs.BatchSettingsRequest
                    {
                        Channel = ParseChannel(parsed.Channel),
                        Intent = batch.Intent
                    });
                    session.Channel = parsed.Channel;
                }
                batch = await _outreach.GetAsync(batch.OutreachBatchId);

                if (batch == null)
                {
                    response.Text = "I couldn't assemble that batch. Try different signals or a wider window.";
                    return;
                }
                if (batch.Members.Count == 0)
                {
                    response.Text = "No one currently matches those signals. Try a wider window or different signals.";
                    return;
                }
                response.Text = $"I found {batch.Members.Count} people worth considering: " +
                    string.Join("; ", batch.Members.Take(5).Select(m => $"{m.PersonName} — {m.Reason}")) +
                    (batch.Members.Count > 5 ? $", and {batch.Members.Count - 5} more." : ".") +
                    " Tell me who to remove, or say continue to set up the message.";
                foreach (var member in batch.Members.Take(8))
                    response.Citations.Add(new CopilotCitation { Kind = "person", Id = member.PersonId, Label = member.PersonName ?? "contact" });
                response.Actions.Add(new AgentAction { Kind = "open-batch", Label = "Review in outreach", Payload = batch.OutreachBatchId.ToString() });
                return;
            }

            var batchState = await _outreach.GetAsync(session.BatchId.Value);

            if (lower.Contains("approve now") && session.PendingApprovals.Any(p => p.StartsWith("approve-batch:")))
            {
                var approved = await _outreach.ApproveAsync(batchState.OutreachBatchId, null);
                session.PendingApprovals.RemoveAll(p => p.StartsWith("approve-batch:"));
                response.Text = $"Approved {approved.Drafts.Count(d => d.Status == Entities.DraftStatus.Approved)} draft(s). Nothing was sent — log each conversation as an interaction when it happens.";
                return;
            }

            if (lower.Contains("approve"))
            {
                var reviewable = batchState.Drafts.Where(d => d.Status == Entities.DraftStatus.Draft || d.Status == Entities.DraftStatus.Edited).ToList();
                if (reviewable.Count == 0)
                {
                    response.Text = "There is nothing ready to approve. Generate drafts first, review them, then approve.";
                    return;
                }
                response.Text = $"Ready to approve {reviewable.Count} draft(s): " +
                    string.Join("; ", reviewable.Take(5).Select(d => $"{d.PersonName} via {d.Channel}")) +
                    ". Approving marks them reviewed — nothing sends by itself. Say \"approve now\" to confirm.";
                session.PendingApprovals.Add("approve-batch:" + batchState.OutreachBatchId);
                response.Actions.Add(new AgentAction { Kind = "approve-batch", Label = $"Approve {reviewable.Count} drafts", Payload = batchState.OutreachBatchId.ToString() });
                return;
            }

            var removed = ExtractQuotedOrKnownNames(lower, batchState);
            if (removed.Count > 0)
            {
                foreach (var name in removed)
                {
                    var member = batchState.Members.FirstOrDefault(m =>
                        string.Equals(m.PersonName, name, StringComparison.OrdinalIgnoreCase) && !m.Excluded);
                    if (member != null)
                        await _outreach.UpdateMemberAsync(batchState.OutreachBatchId, member.OutreachBatchMemberId,
                            new ServiceContracts.DTOs.OutreachDTOs.MemberOverrideRequest { Excluded = true });
                }
                session.SelectedPersonIds.RemoveAll(id => batchState.Members
                    .Where(m => removed.Contains(m.PersonName ?? string.Empty, StringComparer.OrdinalIgnoreCase))
                    .Select(m => m.PersonId).Contains(id));
                batchState = await _outreach.GetAsync(session.BatchId.Value);
                var remaining = batchState.Members.Count(m => !m.Excluded);
                response.Text = remaining == 0
                    ? "Everyone is removed — the batch is empty. Tell me who to add back, or start over."
                    : $"Removed. {remaining} remaining: " +
                      string.Join("; ", batchState.Members.Where(m => !m.Excluded).Take(6).Select(m => m.PersonName)) + ".";
                return;
            }

            var channel = DetectChannel(lower);
            if (channel != null)
            {
                await _outreach.UpdateSettingsAsync(batchState.OutreachBatchId, new ServiceContracts.DTOs.OutreachDTOs.BatchSettingsRequest
                {
                    Channel = channel.Value,
                    Intent = batchState.Intent
                });
                session.Channel = channel.Value.ToString();
                response.Text = $"Channel set to {channel.Value} for everyone without an individual override. What should the messages aim for — or say \"prepare\" and I'll draft them.";
                return;
            }

            if (lower.Contains("prepare") || lower.Contains("draft"))
            {
                var withDrafts = await _outreach.GenerateDraftsAsync(batchState.OutreachBatchId);
                response.Text = $"Prepared {withDrafts.Drafts.Count} personalized draft(s), each grounded in that person's context. Review them in outreach — edit, regenerate, or approve individually.";
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

            if (lower.Contains("regenerate") || lower.Contains("change ") || lower.Contains("instead") || lower.Contains("email for") || lower.Contains("linkedin for"))
            {
                response.Text = "Tell me which person and what to change — for example \"use email for Sarah\" — and I'll regenerate just their draft.";
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
                response.Text = "Applied that instruction to the whole batch. Say \"prepare\" when ready and I'll draft each message.";
                return;
            }

            response.Text = "The batch is waiting. You can remove people, change the channel, give an instruction, or say \"prepare\" to draft.";
        }

        private List<string> ExtractQuotedOrKnownNames(string lower, ServiceContracts.DTOs.OutreachDTOs.OutreachBatchResponse batch)
        {
            var found = new List<string>();
            foreach (var member in batch.Members.Where(m => !m.Excluded))
            {
                var name = (member.PersonName ?? string.Empty).ToLowerInvariant();
                if (name.Length == 0) continue;
                var first = name.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
                if ((first.Length > 2 && lower.Contains("remove " + first))
                    || lower.Contains("without " + first)
                    || lower.Contains("drop " + first)
                    || lower.Contains("exclude " + first)
                    || (!string.IsNullOrWhiteSpace(member.PersonName) && lower.Contains("not " + first)))
                    found.Add(member.PersonName ?? string.Empty);
            }
            return found.Where(n => n.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static Entities.OutreachChannel? DetectChannel(string lower)
        {
            if (lower.Contains("linkedin")) return Entities.OutreachChannel.LinkedIn;
            if (lower.Contains("email") && (lower.Contains("use ") || lower.Contains("via ") || lower.Contains("change ") || lower.Contains("instead"))) return Entities.OutreachChannel.Email;
            if (lower.Contains("text") && (lower.Contains("use ") || lower.Contains("via "))) return Entities.OutreachChannel.Text;
            if (lower.Contains("call") && (lower.Contains("prepare") || lower.Contains("brief"))) return Entities.OutreachChannel.CallPrep;
            return null;
        }

        private static Entities.OutreachChannel ParseChannel(string channel) => channel switch
        {
            "LinkedIn" => Entities.OutreachChannel.LinkedIn,
            "Text" => Entities.OutreachChannel.Text,
            "CallPrep" => Entities.OutreachChannel.CallPrep,
            _ => Entities.OutreachChannel.Email
        };

        private async Task HandleMeetingPrepAsync(AgentSession session, List<string> mentioned, AgentResponse response)
        {
            if (mentioned.Count == 0)
            {
                response.Text = "Who are you meeting? Give me names and I'll map them to your contacts — I won't invent participants.";
                response.NeedsInput = new AgentClarification { Prompt = "Who are you meeting?", Options = new List<string>() };
                session.CurrentTask = "meeting-prep";
                return;
            }

            var mapped = new List<string>();
            var missing = new List<string>();
            foreach (var name in mentioned.Distinct(StringComparer.OrdinalIgnoreCase).Take(10))
            {
                var candidates = await ResolveAsync(name);
                if (candidates.Count == 1) mapped.Add(name);
                else missing.Add(name);
            }
            if (missing.Count > 0)
            {
                response.Text = mapped.Count == 0
                    ? $"I couldn't match {string.Join(", ", missing)} to anyone in your network. Check the spelling, or add them as contacts first."
                    : $"I matched {string.Join(", ", mapped)} but not {string.Join(", ", missing)}. Who did you mean?";
                if (mapped.Count > 0)
                    response.NeedsInput = new AgentClarification { Prompt = "Who did you mean?", Options = new List<string>() };
                session.CurrentTask = "meeting-prep";
                return;
            }

            response.Text = $"I can prepare for a meeting with {string.Join(", ", mapped)}. Give me a title and date — for example \"Partnership review tomorrow at 10\" — and I'll create the preparation.";
            session.CurrentTask = "meeting-prep";
            session.PendingApprovals.Add("meeting-participants:" + string.Join(",", mapped));
            response.Actions.Add(new AgentAction { Kind = "prepare-meeting", Label = "Start meeting preparation", Payload = string.Join(",", mapped) });
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

            if (session.SelectedPersonIds.Count > 0 || session.PersonId != null)
            {
                var people = await _persons.GetAllPersons();
                var names = people.Where(p => p != null).ToDictionary(p => p!.PersonId, p => p!.Name);
                if (session.PersonId != null && names.TryGetValue(session.PersonId.Value, out var single))
                    state.SelectedPeople.Add(single ?? "contact");
                foreach (var id in session.SelectedPersonIds)
                    state.SelectedPeople.Add(names.TryGetValue(id, out var name) ? name ?? "contact" : "contact");
            }
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
