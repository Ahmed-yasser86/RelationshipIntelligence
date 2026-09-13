using Microsoft.SemanticKernel;
using ServiceContracts;
using System;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace RelationshipIntelligence.AI
{
    /// <summary>
    /// Read-only co-pilot tools. Every tool flows through the existing
    /// owner-scoped application services, so user isolation is inherited —
    /// the agent can only ever see the caller's own data.
    /// </summary>
    public sealed class RelationshipPlugin
    {
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

        public RelationshipPlugin(
            IRelationshipScoringService scoring,
            IInteractionService interactions,
            IRelationshipMemoryService memory,
            IEventService events,
            INetworkAnalysisService network,
            IPersonSearcherService searcher)
        {
            _scoring = scoring;
            _interactions = interactions;
            _memory = memory;
            _events = events;
            _network = network;
            _searcher = searcher;
        }

        [KernelFunction, Description("Get the scored relationship state for one person: band, urgency, cadence, silence, strength, evidence status.")]
        public async Task<string> GetRelationshipStateAsync(
            [Description("The person's id (Guid).")] string personId)
        {
            if (!Guid.TryParse(personId, out var id))
                return JsonSerializer.Serialize(new { error = "Invalid person id." }, Json);

            var queue = await _scoring.GetQueueAsync(200);
            var row = queue.FirstOrDefault(q => q.PersonId == id);
            if (row == null)
                return JsonSerializer.Serialize(new { observed = false, note = "No scored state. The person may be unscored or unknown." }, Json);

            return JsonSerializer.Serialize(row, Json);
        }

        [KernelFunction, Description("List logged interactions for one person, newest last. These are the observed evidence.")]
        public async Task<string> GetInteractionsAsync(
            [Description("The person's id (Guid).")] string personId,
            [Description("Maximum interactions to return.")] int limit = 20)
        {
            if (!Guid.TryParse(personId, out var id))
                return JsonSerializer.Serialize(new { error = "Invalid person id." }, Json);

            var rows = await _interactions.ListForPersonAsync(id);
            return JsonSerializer.Serialize(rows.Take(Math.Clamp(limit, 1, 100)), Json);
        }

        [KernelFunction, Description("Get active relationship memory for one person, with provenance labels (user note, confirmed, meeting-derived, suggested).")]
        public async Task<string> GetRelationshipMemoryAsync(
            [Description("The person's id (Guid).")] string personId)
        {
            if (!Guid.TryParse(personId, out var id))
                return JsonSerializer.Serialize(new { error = "Invalid person id." }, Json);

            var entries = await _memory.ListForPersonAsync(id);
            return JsonSerializer.Serialize(entries, Json);
        }

        [KernelFunction, Description("Get upcoming event occurrences across the network with person and silence context.")]
        public async Task<string> GetUpcomingEventsAsync(
            [Description("Window in days (1-60).")] int days = 21)
        {
            var occurrences = await _events.GetUpcomingAsync(Math.Clamp(days, 1, 60));
            return JsonSerializer.Serialize(occurrences, Json);
        }

        [KernelFunction, Description("Get the attention queue: ranked relationships with reasons, bands, and event signals.")]
        public async Task<string> GetAttentionQueueAsync(
            [Description("Maximum rows to return.")] int top = 10)
        {
            var queue = await _scoring.GetQueueAsync(Math.Clamp(top, 1, 50));
            return JsonSerializer.Serialize(queue, Json);
        }

        [KernelFunction, Description("Get network context for one person: neighbors, shared-context reasons, bridge flag. Edges are shared context, not observed contact.")]
        public async Task<string> GetNetworkContextAsync(
            [Description("The person's id (Guid).")] string personId)
        {
            if (!Guid.TryParse(personId, out var id))
                return JsonSerializer.Serialize(new { error = "Invalid person id." }, Json);

            var graph = await _network.GetGraphAsync();
            var node = graph.Nodes.FirstOrDefault(n => n.PersonId == id);
            if (node == null)
                return JsonSerializer.Serialize(new { observed = false, note = "Person not present in the network graph." }, Json);

            var edges = graph.Edges.Where(e => e.From == id || e.To == id).ToList();
            var names = graph.Nodes.ToDictionary(n => n.PersonId, n => n.Name);
            return JsonSerializer.Serialize(new
            {
                node,
                neighbors = edges.Select(e => new
                {
                    personId = e.From == id ? e.To : e.From,
                    name = names.TryGetValue(e.From == id ? e.To : e.From, out var name) ? name : "Unnamed contact",
                    reason = e.Reason
                })
            }, Json);
        }

        [KernelFunction, Description("Search people by name. Returns candidates with ids, organizations, and roles for mapping confirmation.")]
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
    }
}
