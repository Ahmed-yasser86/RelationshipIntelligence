using Servicess;
using ServiceContracts;
using ServiceContracts.DTOs.CopilotDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RelationshipIntelligence.AI
{
    /// <summary>
    /// Deterministic co-pilot used when Copilot:Mode is Stub (development, e2e).
    /// Drafts are composed reason-first from the caller's real context — never invented.
    /// </summary>
    public sealed class StubCopilotService : ICopilotService
    {
        private readonly IRelationshipScoringService _scoring;
        private readonly IInteractionService _interactions;
        private readonly IRelationshipMemoryService _memory;
        private readonly IEventService _events;
        private readonly IPersonGetterService _persons;

        public StubCopilotService(
            IRelationshipScoringService scoring,
            IInteractionService interactions,
            IRelationshipMemoryService memory,
            IEventService events,
            IPersonGetterService persons)
        {
            _scoring = scoring;
            _interactions = interactions;
            _memory = memory;
            _events = events;
            _persons = persons;
        }

        public async Task<CopilotAnswer> AskAsync(string question, Guid? personId, List<ChatTurnDto>? history)
        {
            if (personId == null)
            {
                var queue = await _scoring.GetQueueAsync(5);
                var upcoming = await _events.GetUpcomingAsync(14);
                if (queue.Count == 0)
                    return new CopilotAnswer { Text = "Every relationship is within its natural rhythm, and no dates are approaching. Nothing is asking for action right now." };

                var sb = new StringBuilder();
                sb.Append("This week deserves attention in ");
                sb.Append(queue.Count == 1 ? "one place" : $"{Math.Min(3, queue.Count)} places");
                sb.Append(": ");
                sb.Append(string.Join("; ", queue.Take(3).Select(q =>
                    $"{q.Name} is {q.Band.ToLowerInvariant()} (urgency {Math.Round(q.UrgencyScore)})" +
                    (q.LastContactAtUtc == null ? " with no contact recorded" :
                    $", last contact {(q.SilenceDays ?? TieDecayModel.SilenceDays(q.LastContactAtUtc, DateTime.UtcNow))}d ago" +
                    (q.CadenceReferenceDays == null ? "" : $" against a ~{Math.Round(q.CadenceReferenceDays.Value)}d rhythm")))));
                sb.Append(".");
                if (upcoming.Count > 0)
                {
                    var first = upcoming[0];
                    sb.Append($" Also coming up: {first.Title} for {first.PersonName ?? "a contact"} " +
                        (first.InDays == 0 ? "today." : $"in {first.InDays}d."));
                }
                return new CopilotAnswer { Text = sb.ToString() };
            }

            var person = await _persons.GetPersonByPersonId(personId.Value);
            if (person == null)
                return new CopilotAnswer { Text = "Not recorded — I have no contact with that id." };

            var interactions = await _interactions.ListForPersonAsync(personId.Value);
            var last = interactions.OrderByDescending(i => i.TimeOfInteraction).FirstOrDefault();
            var memory = await _memory.ListForPersonAsync(personId.Value);
            var active = memory.Where(m => m.Status == 0).ToList();
            var commitments = active.Where(m => m.Kind == Entities.RelationshipMemoryKind.Commitment).ToList();

            var answer = new StringBuilder();
            var displayName = person.Name ?? "This contact";
            if (last == null)
            {
                answer.Append($"{displayName} has no logged contact yet, so there is no rhythm to read. ");
            }
            else
            {
                var days = TieDecayModel.SilenceDays(last.TimeOfInteraction, DateTime.UtcNow);
                answer.Append($"{displayName} was last in touch {days}d ago ({last.InteractionTitle}). ");
                answer.Append($"Their history holds {interactions.Count} logged interaction(s). ");
            }
            if (commitments.Count > 0)
                answer.Append($"Open commitments: {string.Join("; ", commitments.Take(3).Select(c => c.Title))}. ");
            else if (active.Count > 0)
                answer.Append($"Recorded context: {string.Join("; ", active.Take(3).Select(m => m.Title))}. ");
            else
                answer.Append("No relationship context is recorded yet — adding what this relationship is would sharpen every future answer. ");

            return new CopilotAnswer
            {
                Text = answer.ToString().Trim(),
                Citations = new List<CopilotCitation>
                {
                    new() { Kind = "person", Id = personId, Label = person.Name ?? "contact" }
                }
            };
        }

        public async Task<CopilotAnswer> SummarizePersonAsync(Guid personId)
        {
            var person = await _persons.GetPersonByPersonId(personId);
            if (person == null)
                return new CopilotAnswer { Text = "Not recorded — I have no contact with that id." };

            var queue = await _scoring.GetQueueAsync(200);
            var state = queue.FirstOrDefault(q => q.PersonId == personId);
            var base_ = await AskAsync("Summarize this relationship.", personId, null);
            var prefix = state == null
                ? $"{person.Name ?? "This contact"} is unscored — no rhythm measured yet. "
                : $"{person.Name ?? "This contact"} sits at {state.Band.ToLowerInvariant()} with urgency {Math.Round(state.UrgencyScore)} out of 100. ";
            return new CopilotAnswer
            {
                Text = "Briefing — " + prefix + base_.Text,
                Citations = base_.Citations
            };
        }

        public async Task<BriefingDto> BuildBriefingAsync()
        {
            var queue = await _scoring.GetQueueAsync(10);
            var upcoming = await _events.GetUpcomingAsync(21);
            var briefing = new BriefingDto
            {
                GeneratedAtUtc = DateTime.UtcNow,
                AttentionNow = queue.Take(5).Select(q => new BriefingAttentionItem
                {
                    PersonId = q.PersonId,
                    Name = q.Name,
                    Band = q.Band,
                    Reason = $"Urgency {Math.Round(q.UrgencyScore)}/100 ({q.Band})."
                }).ToList(),
                UpcomingEvents = upcoming.Take(8).Select(e => new BriefingEventItem
                {
                    PersonId = e.PersonId,
                    PersonName = e.PersonName,
                    Title = e.Title,
                    InDays = e.InDays,
                    SilenceLine = e.InDays <= 7 ? "coming up soon" : "on the horizon"
                }).ToList(),
                Summary = queue.Count == 0 && upcoming.Count == 0
                    ? "A quiet network — nothing outside its rhythm and no dates approaching."
                    : $"{queue.Count} relationship(s) in the queue; {upcoming.Count} event(s) approaching."
            };
            // Evidence-based actions + trajectory (never empty when queue isn't).
            briefing.SuggestedActions = queue.Take(5).Select(q =>
            {
                var silent = q.SilenceDays ?? TieDecayModel.SilenceDays(q.LastContactAtUtc, DateTime.UtcNow);
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
                    Detail = $"Quiet {(q.SilenceDays ?? TieDecayModel.SilenceDays(q.LastContactAtUtc, DateTime.UtcNow))}d, " +
                        $"longer than {Math.Round((q.SilenceQuantile ?? 0) * 100)}% of past gaps."
                }).ToList();
            return briefing;
        }

        public async Task<PlanSuggestionDto> SuggestPlanAsync(Guid personId, Guid? intentEntryId)
        {
            var person = await _persons.GetPersonByPersonId(personId);
            var entries = await _memory.ListForPersonAsync(personId);
            var intent = intentEntryId != null
                ? entries.FirstOrDefault(e => e.MemoryEntryId == intentEntryId)
                : entries.FirstOrDefault(e => e.Status == 0 && (e.Kind == Entities.RelationshipMemoryKind.Intent || e.Kind == Entities.RelationshipMemoryKind.Goal));

            var queue = await _scoring.GetQueueAsync(200);
            var state = queue.FirstOrDefault(q => q.PersonId == personId);

            var actions = new List<PlanActionDto>();
            if (state != null && state.UrgencyScore > 65)
                actions.Add(new PlanActionDto
                {
                    Action = $"Reconnect with {person?.Name ?? "this contact"} this week.",
                    PersonId = personId,
                    PersonName = person?.Name,
                    WhyNow = $"Urgency {Math.Round(state.UrgencyScore)}/100 ({state.Band}).",
                    Outcome = intent?.Title ?? "Back within rhythm"
                });
            else
                actions.Add(new PlanActionDto
                {
                    Action = "No urgent action — keep logging interactions as they happen.",
                    PersonId = personId,
                    PersonName = person?.Name,
                    WhyNow = state == null ? "No scored state yet." : $"Urgency {Math.Round(state.UrgencyScore)}/100 ({state.Band}).",
                    Outcome = intent?.Title ?? "Steady relationship"
                });

            return new PlanSuggestionDto
            {
                PersonId = personId,
                Intent = intent?.Title ?? "Maintain a healthy rhythm",
                SuggestedActions = actions,
                LimitedContext = entries.Count == 0
            };
        }

        public Task<BatchIntentDto> ParseOutreachIntentAsync(string text)
        {
            return Task.FromResult(OutreachIntentMatcher.Match(text ?? string.Empty));
        }

        public Task<DraftCommunicationResult> DraftCommunicationAsync(DraftCommunicationRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var p = request.Person;
            var signals = new List<string>();
            signals.AddRange(p.RecentInteractions.Take(2));
            signals.AddRange(p.MemoryHighlights.Take(2));
            signals.AddRange(p.UpcomingEvents.Take(2));
            signals.AddRange(p.OpenCommitments.Take(2));
            // Personalization grounding: style guidance stays visible so
            // voice-matching never becomes unexplained hallucination.
            signals.AddRange(p.CommunicationStyle.Take(2).Select(s => $"Style: {s}"));
            signals.AddRange(p.StyleNotes.Take(2));
            signals.AddRange(p.MessageExamples.Take(1).Select(e => $"Wrote before: {Truncate(e, 80)}"));

            string body;
            string? subject = null;
            if (request.Kind == Entities.DraftKind.CallPrep)
            {
                body = BuildCallPrep(p, signals);
            }
            else
            {
                var composed = ComposeMessage(p, request);
                body = composed.Body;
                subject = request.Channel == Entities.OutreachChannel.Email ? composed.Subject : null;
            }

            if (body.Length > 4000)
                body = body[..4000];

            return Task.FromResult(new DraftCommunicationResult
            {
                Subject = subject == null ? null : subject.Length > 200 ? subject[..200] : subject,
                Body = body,
                ContextUsed = signals.Take(8).ToList(),
                LimitedContext = signals.Count == 0
            });
        }

        private sealed record ComposedMessage(string Body, string Subject);

        /// <summary>
        /// Deterministic reason-first composition. Picks the strongest available
        /// reason (open commitment, upcoming event, recent thread, remembered
        /// topic) and expresses it in prose. Raw evidence strings stay in
        /// ContextUsed (grounding) and never leak into the message: no bands,
        /// scores, dates, rhythm language, or bracketed evidence labels.
        /// Personalization: the user's communication profile, approved
        /// style notes, and example messages deterministically shape greeting
        /// and length — different profiles produce materially different drafts.
        /// </summary>
        private static ComposedMessage ComposeMessage(
            ServiceContracts.DTOs.CopilotDTOs.PersonDraftContext p,
            DraftCommunicationRequest request)
        {
            var voice = VoiceFor(p);
            var firstName = p.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? p.Name;
            var greeting = (request.Channel, voice) switch
            {
                (_, Voice.NoGreeting) => string.Empty,
                (Entities.OutreachChannel.Text, Voice.Casual) => $"Hey {firstName} — ",
                (_, Voice.Casual) => $"Hey {firstName}, ",
                (Entities.OutreachChannel.Text, _) => $"Hi {p.Name} — ",
                (Entities.OutreachChannel.LinkedIn, _) => $"Hi {p.Name}, ",
                (_, _) => $"Hi {p.Name},\n\n"
            };

            var instruction = string.IsNullOrWhiteSpace(request.CustomInstruction)
                ? request.GlobalInstruction
                : request.CustomInstruction;
            var lead = string.IsNullOrWhiteSpace(instruction) ? null : WithPeriod(instruction.Trim());

            string reason;
            string subject;
            if (p.OpenCommitments.Count > 0)
            {
                // Verbatim title: commitments are usually imperative ("Send X");
                // lowercasing the verb would manufacture ungrammatical prose.
                var title = p.OpenCommitments[0].Trim();
                reason = $"I wanted to follow up on {title}. Would you have some time this week to go through it together?";
                subject = $"Following up: {Truncate(title, 60)}";
            }
            else if (p.UpcomingEvents.Count > 0 && TrySplitEvent(p.UpcomingEvents[0], out var eventTitle, out var eventDays))
            {
                reason = $"I saw {eventTitle} is coming up {RelativeWhen(eventDays)} and wanted to check in beforehand. Would be good to catch up around it if you'll be there.";
                subject = $"{eventTitle} coming up";
            }
            else if (FirstActionableMemory(p) is { } intent)
            {
                // A recorded intent/goal outranks generic thread continuity:
                // it is the user's own stated reason to re-engage.
                reason = $"One thing I wanted to raise: {intent}. Would be good to get your perspective when you have a moment.";
                subject = "Catching up";
            }
            else if (p.RecentInteractions.Count > 0)
            {
                var title = LowerLead(StripInteractionPrefix(p.RecentInteractions[0]));
                reason = $"I've been thinking about our {title} and wanted to pick up the thread. I'd like to hear how things have moved on your side since.";
                subject = $"Thinking about our {StripInteractionPrefix(p.RecentInteractions[0]).Trim()}";
            }
            else if (FirstBackgroundMemory(p) is { } topic)
            {
                reason = $"I've been thinking about {LowerLead(topic)} lately. Would be good to hear your take when you have a moment.";
                subject = "Catching up";
            }
            else
            {
                reason = "It's been a while since we last spoke, and I wanted to check in. Would be nice to catch up properly when you have a moment.";
                subject = "Checking in";
            }

            // A short voice compresses to the first sentence — the user's own
            // brevity preference, not a template change.
            if (voice == Voice.Short)
                reason = FirstSentence(reason);

            string body;
            if (request.Channel == Entities.OutreachChannel.Text)
            {
                var single = lead ?? FirstSentence(reason);
                body = $"{greeting}{single}";
            }
            else if (request.Channel == Entities.OutreachChannel.LinkedIn)
            {
                body = lead == null ? $"{greeting}{reason}" : $"{greeting}{lead} {reason}";
            }
            else
            {
                var paragraph = lead == null ? reason : $"{lead} {reason}";
                body = $"{greeting}{paragraph}".Trim();
            }
            return new ComposedMessage(body, subject);
        }

        private enum Voice { Default, Casual, NoGreeting, Short }

        /// <summary>
        /// Reads the deterministic voice from profile + approved style notes +
        /// examples. Explicit user guidance wins; examples only inform
        /// greeting/length when no explicit profile exists.
        /// </summary>
        private static Voice VoiceFor(ServiceContracts.DTOs.CopilotDTOs.PersonDraftContext p)
        {
            var lines = p.CommunicationStyle.Concat(p.StyleNotes).ToList();
            var text = string.Join("\n", lines).ToLowerInvariant();
            if (text.Contains("no greeting") || text.Contains("no formal greeting"))
                return Voice.NoGreeting;
            if (text.Contains("short"))
                return Voice.Short;
            if (text.Contains("casual") || text.Contains("hey") || text.Contains("direct"))
                return Voice.Casual;

            var example = p.MessageExamples.FirstOrDefault();
            if (example != null)
            {
                var trimmed = example.TrimStart();
                if (trimmed.StartsWith("Hey ", StringComparison.OrdinalIgnoreCase))
                    return Voice.Casual;
                if (!StartsWithGreetingWord(trimmed) && trimmed.Length < 120)
                    return Voice.Short;
                if (CountWords(example) < 25)
                    return Voice.Short;
            }
            return Voice.Default;
        }

        private static bool StartsWithGreetingWord(string text) =>
            text.StartsWith("Hi ", StringComparison.OrdinalIgnoreCase)
            || text.StartsWith("Hi,", StringComparison.OrdinalIgnoreCase)
            || text.StartsWith("Hey ", StringComparison.OrdinalIgnoreCase)
            || text.StartsWith("Hello ", StringComparison.OrdinalIgnoreCase)
            || text.StartsWith("Dear ", StringComparison.OrdinalIgnoreCase);

        private static int CountWords(string text) =>
            text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

        private static string BuildCallPrep(
            ServiceContracts.DTOs.CopilotDTOs.PersonDraftContext p,
            List<string> signals)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Call prep — {p.Name}");
            sb.AppendLine();
            sb.AppendLine("Before the call:");
            sb.AppendLine($"- Relationship: {(p.Band == null ? "no scored state" : $"{p.Band}, urgency {(p.UrgencyScore == null ? "?" : Math.Round(p.UrgencyScore.Value).ToString())}")}.");
            if (!string.IsNullOrWhiteSpace(p.CadenceLine)) sb.AppendLine($"- Rhythm: {p.CadenceLine}");
            foreach (var s in signals.Take(4)) sb.AppendLine($"- {s}");
            sb.AppendLine();
            sb.AppendLine("During the call:");
            foreach (var c in p.OpenCommitments.Take(3)) sb.AppendLine($"- Clarify commitment: {c}");
            if (p.OpenCommitments.Count == 0) sb.AppendLine("- No open commitments recorded — ask what matters most right now.");
            sb.AppendLine();
            sb.AppendLine("After the call:");
            sb.AppendLine("- Log the call as an interaction.");
            sb.AppendLine("- Record any new commitments in relationship memory.");
            return sb.ToString().Trim();
        }

        /// <summary>Interaction strings look like "2026-09-01 [Call] Title".</summary>
        private static string StripInteractionPrefix(string raw)
        {
            var close = raw.IndexOf(']');
            var title = close >= 0 ? raw[(close + 1)..] : raw;
            return string.IsNullOrWhiteSpace(title) ? raw.Trim() : title.Trim();
        }

        /// <summary>Memory strings look like "[Kind/Provenance] Title".</summary>
        private static string StripMemoryPrefix(string raw) => SplitMemory(raw).Title;

        private static (string Kind, string Title) SplitMemory(string raw)
        {
            var title = raw.Trim();
            var kind = string.Empty;
            if (title.StartsWith('['))
            {
                var close = title.IndexOf(']');
                if (close > 1)
                {
                    var head = title[1..close];
                    kind = head.Split('/')[0].Trim();
                    title = title[(close + 1)..].Trim();
                }
            }
            if (string.IsNullOrWhiteSpace(title))
                title = raw.Trim();
            return (kind, title);
        }

        private static bool IsActionableKind(string kind) =>
            kind.Equals("Intent", StringComparison.OrdinalIgnoreCase) ||
            kind.Equals("Goal", StringComparison.OrdinalIgnoreCase) ||
            kind.Equals("Commitment", StringComparison.OrdinalIgnoreCase);

        private static string? FirstActionableMemory(
            ServiceContracts.DTOs.CopilotDTOs.PersonDraftContext p)
        {
            foreach (var raw in p.MemoryHighlights)
            {
                var (kind, title) = SplitMemory(raw);
                if (IsActionableKind(kind))
                    return title;
            }
            return null;
        }

        private static string? FirstBackgroundMemory(
            ServiceContracts.DTOs.CopilotDTOs.PersonDraftContext p)
        {
            foreach (var raw in p.MemoryHighlights)
            {
                var (kind, title) = SplitMemory(raw);
                if (!IsActionableKind(kind))
                    return title;
            }
            return null;
        }

        /// <summary>Event strings look like "Title in 4d".</summary>
        private static bool TrySplitEvent(string raw, out string title, out int days)
        {
            title = raw.Trim();
            days = -1;
            var match = System.Text.RegularExpressions.Regex.Match(title, @"^(.*)\s+in\s+(\d+)d\s*$");
            if (!match.Success)
                return false;
            title = match.Groups[1].Value.Trim();
            days = int.Parse(match.Groups[2].Value);
            return title.Length > 0;
        }

        private static string RelativeWhen(int days) =>
            days switch
            {
                < 0 => "soon",
                0 => "today",
                1 => "tomorrow",
                <= 7 => "in a few days",
                <= 14 => "next week",
                _ => "soon"
            };

        private static string LowerLead(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            return char.ToLowerInvariant(text[0]) + text[1..];
        }

        private static string WithPeriod(string text) =>
            text.EndsWith('.') || text.EndsWith('!') || text.EndsWith('?') ? text : text + ".";

        private static string FirstSentence(string text)
        {
            var end = text.IndexOf(". ", StringComparison.Ordinal);
            return (end >= 0 ? text[..(end + 1)] : text).Trim();
        }

        private static string Truncate(string text, int max) =>
            text.Length <= max ? text : text[..max];
    }
}
