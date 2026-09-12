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
                var top = queue.Count == 0
                    ? "The attention queue is empty — every relationship is within its rhythm."
                    : "Top of your attention queue: " + string.Join("; ",
                        queue.Take(3).Select(q => $"{q.Name} ({q.Band}, urgency {Math.Round(q.UrgencyScore)})"));
                return new CopilotAnswer { Text = $"[Observed] {top}" };
            }

            var person = await _persons.GetPersonByPersonId(personId.Value);
            if (person == null)
                return new CopilotAnswer { Text = "Not recorded — I have no contact with that id." };

            var interactions = await _interactions.ListForPersonAsync(personId.Value);
            var last = interactions.OrderByDescending(i => i.TimeOfInteraction).FirstOrDefault();
            var memory = await _memory.ListForPersonAsync(personId.Value);
            var active = memory.Where(m => m.Status == 0).ToList();

            var sb = new StringBuilder();
            sb.Append($"[Observed] {person.Name}: {interactions.Count} logged interaction(s)");
            sb.Append(last == null ? ", none yet." : $", last contact {last.TimeOfInteraction:yyyy-MM-dd} ({last.InteractionTitle}).");
            sb.Append(active.Count == 0
                ? " [Suggested] No relationship context recorded — add what this relationship is so future answers improve."
                : $" [Observed] Recorded context: {string.Join("; ", active.Take(3).Select(m => m.Title))}.");

            return new CopilotAnswer
            {
                Text = sb.ToString(),
                Citations = new List<CopilotCitation>
                {
                    new() { Kind = "person", Id = personId, Label = person.Name ?? "contact" }
                }
            };
        }

        public async Task<CopilotAnswer> SummarizePersonAsync(Guid personId)
        {
            var answer = await AskAsync("Summarize this relationship.", personId, null);
            answer.Text = answer.Text.Replace("[Observed] ", "[Observed] Briefing — ");
            return answer;
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
    }
}
