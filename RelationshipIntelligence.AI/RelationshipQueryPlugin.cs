using Microsoft.SemanticKernel;
using Servicess;
using ServiceContracts;
using System;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace RelationshipIntelligence.AI
{
    /// <summary>
    /// Read-only relationship capabilities. Each tool returns domain-level
    /// evidence (never raw EF entities) and inherits user isolation from the
    /// underlying owner-scoped services. No tool here changes any data.
    /// </summary>
    public sealed class RelationshipQueryPlugin
    {
        // Camel-case wire format: the live agent parses personId/name exactly.
        private static readonly JsonSerializerOptions Json = new()
        {
            WriteIndented = false,
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        };

        private readonly IRelationshipScoringService _scoring;
        private readonly IInteractionService _interactions;
        private readonly IRelationshipMemoryService _memory;
        private readonly IEventService _events;
        private readonly INetworkAnalysisService _network;
        private readonly IPersonSearcherService _searcher;
        private readonly IPersonGetterService _persons;
        private readonly IMeetingService _meetings;
        private readonly IDigestService _digest;
        private readonly IRelationshipPreferenceService _preferences;

        public RelationshipQueryPlugin(
            IRelationshipScoringService scoring,
            IInteractionService interactions,
            IRelationshipMemoryService memory,
            IEventService events,
            INetworkAnalysisService network,
            IPersonSearcherService searcher,
            IPersonGetterService persons,
            IMeetingService meetings,
            IDigestService digest,
            IRelationshipPreferenceService preferences)
        {
            _scoring = scoring;
            _interactions = interactions;
            _memory = memory;
            _events = events;
            _network = network;
            _searcher = searcher;
            _persons = persons;
            _meetings = meetings;
            _digest = digest;
            _preferences = preferences;
        }

        private static Guid? ParseId(string value) =>
            Guid.TryParse(value, out var id) ? id : null;

        [KernelFunction, Description("Get one person's contact record: identity, organizations, roles, channels, tags, notes. Read-only.")]
        public async Task<string> GetPersonAsync(
            [Description("The person's id (Guid).")] string personId)
        {
            var id = ParseId(personId);
            if (id == null)
                return JsonSerializer.Serialize(new { error = "Invalid person id." }, Json);
            var person = await _persons.GetPersonByPersonId(id);
            if (person == null)
                return JsonSerializer.Serialize(new { observed = false, note = "No such contact." }, Json);
            return JsonSerializer.Serialize(person, Json);
        }

        [KernelFunction, Description("Get active relationship memory for one person: facts, goals, commitments, intent, with provenance. Read-only.")]
        public async Task<string> GetRelationshipContextAsync(
            [Description("The person's id (Guid).")] string personId)
        {
            var id = ParseId(personId);
            if (id == null)
                return JsonSerializer.Serialize(new { error = "Invalid person id." }, Json);
            var entries = await _memory.ListForPersonAsync(id.Value);
            return JsonSerializer.Serialize(entries.Where(e => e.Status == 0), Json);
        }

        [KernelFunction, Description("Get the scored relationship state: band, urgency, cadence, silence, strength, evidence status. Deterministic model output. Read-only.")]
        public async Task<string> GetRelationshipStateAsync(
            [Description("The person's id (Guid).")] string personId)
        {
            var id = ParseId(personId);
            if (id == null)
                return JsonSerializer.Serialize(new { error = "Invalid person id." }, Json);
            var queue = await _scoring.GetQueueAsync(200);
            var row = queue.FirstOrDefault(q => q.PersonId == id.Value);
            if (row == null)
                return JsonSerializer.Serialize(new { observed = false, note = "No scored state. The person may be unscored or unknown." }, Json);
            return JsonSerializer.Serialize(row, Json);
        }

        [KernelFunction, Description("List logged interactions for one person, newest last. The observed evidence. Read-only.")]
        public async Task<string> GetInteractionHistoryAsync(
            [Description("The person's id (Guid).")] string personId,
            [Description("Maximum interactions to return.")] int limit = 20)
        {
            var id = ParseId(personId);
            if (id == null)
                return JsonSerializer.Serialize(new { error = "Invalid person id." }, Json);
            var rows = await _interactions.ListForPersonAsync(id.Value);
            return JsonSerializer.Serialize(rows.Take(Math.Clamp(limit, 1, 100)), Json);
        }

        [KernelFunction, Description("Get the scored trajectory history (urgency/strength over time) for one person. Read-only.")]
        public async Task<string> GetRelationshipTimelineAsync(
            [Description("The person's id (Guid).")] string personId)
        {
            var id = ParseId(personId);
            if (id == null)
                return JsonSerializer.Serialize(new { error = "Invalid person id." }, Json);
            var history = await _scoring.GetHistoryAsync(id.Value);
            return JsonSerializer.Serialize(history, Json);
        }

        [KernelFunction, Description("Get upcoming event occurrences with person and silence context. Read-only.")]
        public async Task<string> GetUpcomingEventsAsync(
            [Description("Window in days (1-60).")] int days = 21)
        {
            var occurrences = await _events.GetUpcomingAsync(Math.Clamp(days, 1, 60));
            return JsonSerializer.Serialize(occurrences, Json);
        }

        [KernelFunction, Description("Get the attention queue: ranked relationships with reasons, bands, and event signals. Deterministic ranking. Read-only.")]
        public async Task<string> GetAttentionItemsAsync(
            [Description("Maximum rows to return.")] int top = 10)
        {
            var queue = await _scoring.GetQueueAsync(Math.Clamp(top, 1, 50));
            return JsonSerializer.Serialize(queue, Json);
        }

        [KernelFunction, Description("Get active goals and intents for one person. Read-only.")]
        public async Task<string> GetRelationshipGoalsAsync(
            [Description("The person's id (Guid).")] string personId)
        {
            var id = ParseId(personId);
            if (id == null)
                return JsonSerializer.Serialize(new { error = "Invalid person id." }, Json);
            var entries = await _memory.ListForPersonAsync(id.Value);
            return JsonSerializer.Serialize(entries.Where(e => e.Status == 0 && (e.Kind == Entities.RelationshipMemoryKind.Goal || e.Kind == Entities.RelationshipMemoryKind.Intent)), Json);
        }

        [KernelFunction, Description("Get active commitments for one person, optionally across the network when no person is given. Read-only.")]
        public async Task<string> GetCommitmentsAsync(
            [Description("The person's id (Guid), or empty for the whole network.")] string personId = "")
        {
            if (ParseId(personId) is Guid id)
            {
                var entries = await _memory.ListForPersonAsync(id);
                return JsonSerializer.Serialize(entries.Where(e => e.Status == 0 && e.Kind == Entities.RelationshipMemoryKind.Commitment), Json);
            }

            var people = await _persons.GetAllPersons();
            var result = new System.Collections.Generic.List<object>();
            foreach (var person in people.Where(p => p != null).Take(200))
            {
                System.Collections.Generic.List<ServiceContracts.DTOs.MemoryDTOs.MemoryEntryResponse> entries;
                try
                {
                    entries = await _memory.ListForPersonAsync(person!.PersonId);
                }
                catch (KeyNotFoundException)
                {
                    continue;
                }
                foreach (var entry in entries.Where(e => e.Status == 0 && e.Kind == Entities.RelationshipMemoryKind.Commitment).Take(5))
                    result.Add(new { person = person!.Name, entry.Title, entry.Detail });
                if (result.Count >= 30) break;
            }
            return JsonSerializer.Serialize(result, Json);
        }

        [KernelFunction, Description("Get one meeting with participants, findings, brief, and status. Read-only.")]
        public async Task<string> GetMeetingAsync(
            [Description("The meeting id (Guid).")] string meetingId)
        {
            var id = ParseId(meetingId);
            if (id == null)
                return JsonSerializer.Serialize(new { error = "Invalid meeting id." }, Json);
            try
            {
                var meeting = await _meetings.GetAsync(id.Value);
                return JsonSerializer.Serialize(meeting, Json);
            }
            catch (KeyNotFoundException)
            {
                return JsonSerializer.Serialize(new { observed = false, note = "No such meeting." }, Json);
            }
        }

        [KernelFunction, Description("Get meetings logged for one person, newest first. Read-only.")]
        public async Task<string> GetMeetingHistoryAsync(
            [Description("The person's id (Guid).")] string personId)
        {
            var id = ParseId(personId);
            if (id == null)
                return JsonSerializer.Serialize(new { error = "Invalid person id." }, Json);
            try
            {
                var meetings = await _meetings.ListMeetingsForPersonAsync(id.Value);
                return JsonSerializer.Serialize(meetings.Select(m => new
                {
                    m.MeetingId,
                    m.Title,
                    m.OccurredAtUtc,
                    m.ActualOccurredAtUtc,
                    m.Status,
                    people = m.People.Count,
                    findings = m.Findings.Count
                }), Json);
            }
            catch (KeyNotFoundException)
            {
                return JsonSerializer.Serialize(new { observed = false, note = "No such contact." }, Json);
            }
        }

        [KernelFunction, Description("Get network context for one person: neighbors, shared-context reasons, bridge flag. Edges are shared context, not observed contact. Read-only.")]
        public async Task<string> GetNetworkContextAsync(
            [Description("The person's id (Guid).")] string personId)
        {
            var id = ParseId(personId);
            if (id == null)
                return JsonSerializer.Serialize(new { error = "Invalid person id." }, Json);
            var graph = await _network.GetGraphAsync();
            var node = graph.Nodes.FirstOrDefault(n => n.PersonId == id.Value);
            if (node == null)
                return JsonSerializer.Serialize(new { observed = false, note = "Person not present in the network graph." }, Json);
            var edges = graph.Edges.Where(e => e.From == id.Value || e.To == id.Value).ToList();
            var names = graph.Nodes.ToDictionary(n => n.PersonId, n => n.Name);
            return JsonSerializer.Serialize(new
            {
                node,
                neighbors = edges.Select(e => new
                {
                    personId = e.From == id.Value ? e.To : e.From,
                    name = names.TryGetValue(e.From == id.Value ? e.To : e.From, out var name) ? name : "Unnamed contact",
                    reason = e.Reason
                })
            }, Json);
        }

        [KernelFunction, Description("Search people by name. Returns candidates with ids, organizations, and roles for mapping confirmation. Read-only.")]
        public async Task<string> SearchPeopleAsync(
            [Description("Name or organization to search for.")] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return JsonSerializer.Serialize(Array.Empty<object>(), Json);
            var result = await _searcher.SearchPersonsBy_Batched(query.Trim(), "Name", 1, 10);
            return JsonSerializer.Serialize(result.Items.Select(p => new
            {
                p.PersonId,
                p.Name,
                organizations = p.Circles.Select(c => c.Name),
                roles = p.ContactItemRoles.Select(r => r.Role)
            }), Json);
        }

        [KernelFunction, Description("Search relationships by name or organization across the network. Read-only.")]
        public async Task<string> SearchRelationshipsAsync(
            [Description("Name or organization to search for.")] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return JsonSerializer.Serialize(Array.Empty<object>(), Json);
            var trimmed = query.Trim();
            var byName = await _searcher.SearchPersonsBy_Batched(trimmed, "Name", 1, 10);
            var queue = await _scoring.GetQueueAsync(200);
            var states = queue.ToDictionary(q => q.PersonId);
            return JsonSerializer.Serialize(byName.Items.Select(p => new
            {
                p.PersonId,
                p.Name,
                organizations = p.Circles.Select(c => c.Name),
                state = states.TryGetValue(p.PersonId, out var s)
                    ? new { s.Band, urgency = Math.Round(s.UrgencyScore), s.LastContactAtUtc } as object
                    : null
            }), Json);
        }

        [KernelFunction, Description("Find relationships whose silence exceeds their own measured rhythm. Deterministic. Read-only.")]
        public async Task<string> FindRelationshipsOutsideCadenceAsync(
            [Description("Maximum rows to return.")] int top = 10)
        {
            var queue = await _scoring.GetQueueAsync(200);
            var now = DateTime.UtcNow;
            var outside = queue
                .Where(q => q.CadenceReferenceDays != null && q.LastContactAtUtc != null
                    && (now.ToUniversalTime() - q.LastContactAtUtc.Value.ToUniversalTime()).TotalDays > q.CadenceReferenceDays.Value)
                .OrderByDescending(q => (now.ToUniversalTime() - q.LastContactAtUtc!.Value.ToUniversalTime()).TotalDays / Math.Max(1, q.CadenceReferenceDays!.Value))
                .Take(Math.Clamp(top, 1, 50))
                .Select(q => new
                {
                    q.PersonId,
                    q.Name,
                    q.Band,
                    silenceDays = q.SilenceDays ?? TieDecayModel.SilenceDays(q.LastContactAtUtc, now),
                    rhythmDays = Math.Round(q.CadenceReferenceDays!.Value),
                    ratio = Math.Round((now.ToUniversalTime() - q.LastContactAtUtc.Value.ToUniversalTime()).TotalDays / Math.Max(1, q.CadenceReferenceDays.Value), 1)
                });
            return JsonSerializer.Serialize(outside, Json);
        }

        [KernelFunction, Description("Find relationships with recent trajectory movement using consecutive state snapshots. Read-only.")]
        public async Task<string> FindRecentRelationshipChangesAsync(
            [Description("Maximum rows to return.")] int top = 10)
        {
            var queue = await _scoring.GetQueueAsync(200);
            var changes = queue
                .Where(q => q.SilenceQuantile != null)
                .OrderByDescending(q => q.UrgencyScore)
                .Take(Math.Clamp(top, 1, 50))
                .Select(q => new
                {
                    q.PersonId,
                    q.Name,
                    q.Band,
                    urgency = Math.Round(q.UrgencyScore),
                    silenceVsHistory = q.SilenceQuantile == null ? (string?)null :
                        $"longer than {Math.Round(q.SilenceQuantile.Value * 100)}% of past gaps"
                });
            return JsonSerializer.Serialize(changes, Json);
        }

        [KernelFunction, Description("Find important upcoming events combined with relationship silence. Read-only.")]
        public async Task<string> FindUpcomingImportantEventsAsync(
            [Description("Window in days (1-60).")] int days = 14)
        {
            var occurrences = await _events.GetUpcomingAsync(Math.Clamp(days, 1, 60));
            var queue = await _scoring.GetQueueAsync(200);
            var states = queue.ToDictionary(q => q.PersonId);
            return JsonSerializer.Serialize(occurrences
                .OrderByDescending(o => o.Importance)
                .ThenBy(o => o.InDays)
                .Take(20)
                .Select(o => new
                {
                    o.PersonName,
                    o.Title,
                    o.InDays,
                    o.Importance,
                    silence = states.TryGetValue(o.PersonId, out var s) && s.LastContactAtUtc != null
                        ? $"quiet for {(s.SilenceDays ?? TieDecayModel.SilenceDays(s.LastContactAtUtc, DateTime.UtcNow))}d" : "no contact recorded"
                }), Json);
        }

        [KernelFunction, Description("Find unresolved follow-ups: open commitments across the network. Read-only.")]
        public async Task<string> FindPendingFollowUpsAsync()
        {
            var people = await _persons.GetAllPersons();
            var result = new System.Collections.Generic.List<object>();
            foreach (var person in people.Where(p => p != null).Take(200))
            {
                System.Collections.Generic.List<ServiceContracts.DTOs.MemoryDTOs.MemoryEntryResponse> entries;
                try
                {
                    entries = await _memory.ListForPersonAsync(person!.PersonId);
                }
                catch (KeyNotFoundException)
                {
                    continue;
                }
                foreach (var entry in entries.Where(e => e.Status == 0 && e.Kind == Entities.RelationshipMemoryKind.Commitment).Take(5))
                    result.Add(new { person = person!.Name, personId = person.PersonId, entry.Title, entry.Detail });
                if (result.Count >= 30) break;
            }
            return JsonSerializer.Serialize(result, Json);
        }

        [KernelFunction, Description("Explain why a relationship is in the attention queue: full evidence trail in words. Deterministic. Read-only.")]
        public async Task<string> ExplainAttentionSignalAsync(
            [Description("The person's id (Guid).")] string personId)
        {
            var id = ParseId(personId);
            if (id == null)
                return JsonSerializer.Serialize(new { error = "Invalid person id." }, Json);
            var queue = await _scoring.GetQueueAsync(200);
            var row = queue.FirstOrDefault(q => q.PersonId == id.Value);
            if (row == null)
                return JsonSerializer.Serialize(new { observed = false, note = "Not currently in the attention queue." }, Json);
            var silenceExact = row.LastContactAtUtc == null ? (double?)null : (DateTime.UtcNow.ToUniversalTime() - row.LastContactAtUtc.Value.ToUniversalTime()).TotalDays;
            var silenceDays = row.SilenceDays ?? TieDecayModel.SilenceDays(row.LastContactAtUtc, DateTime.UtcNow);
            return JsonSerializer.Serialize(new
            {
                row.Name,
                row.Band,
                urgency = Math.Round(row.UrgencyScore),
                interactions = row.InteractionCount,
                evidence = row.EvidenceStatus,
                rhythm = row.CadenceReferenceDays == null ? "not measured" : $"~{Math.Round(row.CadenceReferenceDays.Value)} days",
                silence = silenceDays == null ? "no contact recorded" : $"{silenceDays} days",
                quantile = row.SilenceQuantile == null ? "not enough history" : $"longer than {Math.Round(row.SilenceQuantile.Value * 100)}% of past gaps",
                bridge = row.IsBridge,
                upcomingEvents = row.UpcomingEvents.Select(e => $"{e.Title} in {e.InDays}d")
            }, Json);
        }

        [KernelFunction, Description("Analyze cadence vs silence for one person: reference rhythm, current silence, quantile, interpretation. Deterministic. Read-only.")]
        public async Task<string> GetCadenceAnalysisAsync(
            [Description("The person's id (Guid).")] string personId)
        {
            var id = ParseId(personId);
            if (id == null)
                return JsonSerializer.Serialize(new { error = "Invalid person id." }, Json);
            var queue = await _scoring.GetQueueAsync(200);
            var row = queue.FirstOrDefault(q => q.PersonId == id.Value);
            if (row == null)
                return JsonSerializer.Serialize(new { observed = false, note = "No scored state." }, Json);
            var silence = row.LastContactAtUtc == null ? (double?)null : (DateTime.UtcNow.ToUniversalTime() - row.LastContactAtUtc.Value.ToUniversalTime()).TotalDays;
            var pastRhythm = silence != null && row.CadenceReferenceDays != null && silence > row.CadenceReferenceDays.Value;
            return JsonSerializer.Serialize(new
            {
                referenceDays = row.CadenceReferenceDays,
                measured = row.EvidenceStatus == "Established",
                silenceDays = row.SilenceDays ?? TieDecayModel.SilenceDays(row.LastContactAtUtc, DateTime.UtcNow),
                pastRhythm,
                ratio = silence != null && row.CadenceReferenceDays != null && row.CadenceReferenceDays.Value > 0
                    ? Math.Round(silence.Value / row.CadenceReferenceDays.Value, 1) : (double?)null,
                interpretation = pastRhythm
                    ? "Silence has passed the usual rhythm; attention is justified by the data."
                    : "Within or without a measured rhythm; silence alone does not justify urgency."
            }, Json);
        }

        [KernelFunction, Description("Summarize how a relationship changed: band, urgency, silence trajectory, recent events. Deterministic. Read-only.")]
        public async Task<string> GetRelationshipChangesAsync(
            [Description("The person's id (Guid).")] string personId)
        {
            var id = ParseId(personId);
            if (id == null)
                return JsonSerializer.Serialize(new { error = "Invalid person id." }, Json);
            var queue = await _scoring.GetQueueAsync(200);
            var row = queue.FirstOrDefault(q => q.PersonId == id.Value);
            if (row == null)
                return JsonSerializer.Serialize(new { observed = false, note = "No scored state." }, Json);
            var history = await _scoring.GetHistoryAsync(id.Value);
            return JsonSerializer.Serialize(new
            {
                row.Name,
                current = new { row.Band, urgency = Math.Round(row.UrgencyScore), row.InteractionCount },
                snapshots = history,
                note = "Band and urgency reflect the latest recomputation; snapshots show the path."
            }, Json);
        }

        [KernelFunction, Description("Get the weekly digest: relationships to protect with suggestions and network health. Deterministic. Read-only.")]
        public async Task<string> GetDigestAsync()
        {
            var payload = await _digest.BuildAsync(string.Empty);
            return JsonSerializer.Serialize(new
            {
                weekStartUtc = payload.WeekStartUtc,
                networkHealth = payload.NetworkHealth,
                entries = payload.Entries.Select(e => new
                {
                    personId = e.Health.PersonId,
                    personName = e.Health.Name,
                    band = e.Health.Band,
                    urgency = Math.Round(e.Health.UrgencyScore),
                    e.Suggestion
                })
            }, Json);
        }

        [KernelFunction, Description("Get one person's relationship preferences and reminder state: desired cadence, importance, priority, intentional contact, suggestion inclusion, reminder interval and snooze. User intent, not relationship verdict. Read-only.")]
        public async Task<string> GetPreferenceAsync(
            [Description("The person's id (Guid).")] string personId)
        {
            var id = ParseId(personId);
            if (id == null)
                return JsonSerializer.Serialize(new { error = "Invalid person id." }, Json);
            try
            {
                var preference = await _preferences.GetAsync(id.Value);
                if (preference == null)
                    return JsonSerializer.Serialize(new { observed = false, note = "No preferences set. Defaults apply." }, Json);
                return JsonSerializer.Serialize(preference, Json);
            }
            catch (KeyNotFoundException)
            {
                return JsonSerializer.Serialize(new { observed = false, note = "No such contact." }, Json);
            }
        }

        [KernelFunction, Description("List due user-configured reminders: who the user asked to be reminded about, at what interval. Intention, not urgency — a due reminder never means the relationship is urgent. Read-only.")]
        public async Task<string> ListDueRemindersAsync()
        {
            var due = await _preferences.ListDueAsync();
            return JsonSerializer.Serialize(due, Json);
        }

        [KernelFunction, Description("Get one person's reminder state machine state: Disabled, Idle, Due, Snoozed, Skipped, or Completed. Derived from stored fields, never a separate copy. Viewing this never records an interaction. Read-only.")]
        public async Task<string> GetReminderStateAsync(
            [Description("The person's id (Guid).")] string personId)
        {
            var id = ParseId(personId);
            if (id == null)
                return JsonSerializer.Serialize(new { error = "Invalid person id." }, Json);
            try
            {
                var state = await _preferences.GetReminderStateAsync(id.Value);
                return JsonSerializer.Serialize(new { personId = id.Value, state }, Json);
            }
            catch (KeyNotFoundException)
            {
                return JsonSerializer.Serialize(new { observed = false, note = "No such contact." }, Json);
            }
        }

        [KernelFunction, Description("Get one person's preference change history: what changed, previous and new values in human terms, when, and through which path (User, Copilot, Import, Default). Read-only.")]
        public async Task<string> GetPreferenceHistoryAsync(
            [Description("The person's id (Guid).")] string personId)
        {
            var id = ParseId(personId);
            if (id == null)
                return JsonSerializer.Serialize(new { error = "Invalid person id." }, Json);
            try
            {
                var history = await _preferences.GetHistoryAsync(id.Value);
                return JsonSerializer.Serialize(history, Json);
            }
            catch (KeyNotFoundException)
            {
                return JsonSerializer.Serialize(new { observed = false, note = "No such contact." }, Json);
            }
        }

        [KernelFunction, Description("Get the user's global preference defaults: fallback cadence and reminder strictness used only when a person has no explicit preference. Read-only.")]
        public async Task<string> GetGlobalDefaultsAsync()
        {
            var defaults = await _preferences.GetGlobalDefaultsAsync();
            return JsonSerializer.Serialize(defaults, Json);
        }

        [KernelFunction, Description("Get unapproved AI proposals awaiting review: suggested memory entries and suggested meeting findings. Nothing here is durable truth. Read-only.")]
        public async Task<string> GetPendingReviewAsync()
        {
            var suggestions = new List<object>();
            var people = await _persons.GetAllPersons();
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
                foreach (var entry in entries
                    .Where(e => e.Status == 0 && e.Provenance == Entities.MemoryProvenance.AiSuggested)
                    .Take(5))
                    suggestions.Add(new
                    {
                        type = "memory",
                        person = person!.Name,
                        personId = person.PersonId,
                        kind = entry.Kind.ToString(),
                        entry.Title
                    });
                if (suggestions.Count >= 30) break;
            }
            var meetings = await _meetings.ListAsync();
            foreach (var meeting in meetings.Take(50))
            {
                foreach (var finding in meeting.Findings
                    .Where(f => f.Status == Entities.FindingStatus.Suggested)
                    .Take(5))
                    suggestions.Add(new
                    {
                        type = "meeting",
                        person = finding.MappedPersonName,
                        personId = finding.MappedPersonId,
                        kind = finding.Kind.ToString(),
                        title = finding.Title
                    });
                if (suggestions.Count >= 50) break;
            }
            return JsonSerializer.Serialize(suggestions, Json);
        }
    }
}
