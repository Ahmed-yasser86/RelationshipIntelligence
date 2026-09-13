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
    /// Answers are templated but built from the caller's real data — never invented.
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
                    $", last contact {(int)(DateTime.UtcNow - q.LastContactAtUtc.Value).TotalDays}d ago" +
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
                var days = (int)(DateTime.UtcNow - last.TimeOfInteraction).TotalDays;
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

            var greeting = request.Channel switch
            {
                Entities.OutreachChannel.Text => $"Hi {p.Name} — ",
                Entities.OutreachChannel.LinkedIn => $"Hi {p.Name}, ",
                _ => $"Hi {p.Name},\n\n"
            };
            var intentLine = string.IsNullOrWhiteSpace(request.CustomInstruction)
                ? request.GlobalInstruction
                : request.CustomInstruction;
            var purpose = string.IsNullOrWhiteSpace(intentLine)
                ? $"I wanted to {request.Intent.ToLowerInvariant()}."
                : intentLine.Trim();

            string body;
            if (request.Kind == Entities.DraftKind.CallPrep)
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
                body = sb.ToString().Trim();
            }
            else if (request.Channel == Entities.OutreachChannel.Text)
            {
                body = $"{greeting}{purpose} " +
                    (signals.Count == 0 ? "Hope you're well!" : signals[0]);
            }
            else
            {
                var sb = new StringBuilder();
                sb.Append(greeting);
                sb.AppendLine(purpose);
                sb.AppendLine();
                foreach (var s in signals.Take(3)) sb.AppendLine($"- {s}");
                sb.AppendLine();
                sb.Append("Would love to catch up properly. Let me know what works for you.");
                body = sb.ToString().Trim();
            }

            if (body.Length > 4000)
                body = body[..4000];

            return Task.FromResult(new DraftCommunicationResult
            {
                Subject = request.Channel == Entities.OutreachChannel.Email
                    ? $"Reconnecting with {p.Name}"[..Math.Min(200, $"Reconnecting with {p.Name}".Length)]
                    : null,
                Body = body,
                ContextUsed = signals.Take(8).ToList(),
                LimitedContext = signals.Count == 0
            });
        }
    }
}
