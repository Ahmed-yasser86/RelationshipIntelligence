using Microsoft.Extensions.Logging;
using RepositryContracts;
using SerilogTimings;
using ServiceContracts;
using ServiceContracts.DTOs;
using Servicess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Servicess
{
    public class NetworkAnalysisService : INetworkAnalysisService
    {
        public const int MaxNodes = 400;
        private readonly PersonRepositryContract _persons;
        private readonly RelationshipStateRepositoryContract _states;
        private readonly ICurrentUserService _currentUser;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<NetworkAnalysisService> _logger;

        public NetworkAnalysisService(
            PersonRepositryContract persons,
            RelationshipStateRepositoryContract states,
            ICurrentUserService currentUser,
            IUnitOfWork unitOfWork,
            ILogger<NetworkAnalysisService> logger)
        {
            _persons = persons;
            _states = states;
            _currentUser = currentUser;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<NetworkGraphResponse> GetGraphAsync()
        {
            using (Operation.Time("Build network graph"))
            {
                var response = new NetworkGraphResponse();
                var userId = _currentUser.UserId;
                if (userId == null || userId == Guid.Empty)
                    return response;

                var states = (await _states.ListForOwnerAsync(userId.Value))
                    .ToDictionary(s => s.PersonId);
                var urgencyOf = new Func<Guid, double>(id =>
                    states.TryGetValue(id, out var s) ? s.UrgencyScore : 0);

                var affinities = await _persons.ListAffinitiesAsync();
                var connected = ConnectedPersonIds(
                    affinities, NetworkAnalyzer.DefaultMaxSharedAttributeMembers);

                var candidateIds = connected
                    .OrderByDescending(urgencyOf)
                    .Take(MaxNodes)
                    .ToList();
                if (candidateIds.Count < 50)
                {
                    foreach (var id in states.Keys.OrderByDescending(urgencyOf))
                    {
                        if (candidateIds.Count >= MaxNodes)
                            break;
                        if (!candidateIds.Contains(id))
                            candidateIds.Add(id);
                    }
                }

                var persons = (await _persons.ListByIdsAsync(candidateIds))
                    .Where(p => p != null)
                    .ToList();

                var analysis = NetworkAnalyzer.Analyze(
                    persons.Select(p => new GraphNodeInput(
                        p!.PersonId,
                        p.Circles.FirstOrDefault()?.Name,
                        JsonSerializer.Serialize(p.UserDefinedTags.Select(t => t.TagName).ToList()),
                        p.ConnectionChannels.FirstOrDefault()?.ConnectionChannelName)).ToList(),
                    new List<IReadOnlyList<Guid>>());

                bool changed = false;
                foreach (var person in persons)
                {
                    bool isBridge = analysis.Bridges.Contains(person!.PersonId);
                    response.Nodes.Add(new NetworkNodeResponse
                    {
                        PersonId = person.PersonId,
                        Name = person.Name,
                        Degree = analysis.Degrees.TryGetValue(person.PersonId, out int d) ? d : 0,
                        IsBridge = isBridge,
                        IsIsolated = !analysis.Degrees.TryGetValue(person.PersonId, out int deg) || deg == 0,
                        UrgencyScore = states.TryGetValue(person.PersonId, out var s) ? s.UrgencyScore : 0,
                        EvidenceStatus = states.TryGetValue(person.PersonId, out var st)
                            ? st.EvidenceStatus.ToString()
                            : Entities.EvidenceStatus.NoHistory.ToString()
                    });

                    if (states.TryGetValue(person.PersonId, out var state) && state.IsBridge != isBridge)
                    {
                        state.IsBridge = isBridge;
                        changed = true;
                    }
                }

                if (changed)
                    await _unitOfWork.SaveChangesAsync();

                response.Edges.AddRange(analysis.Edges.Select(e => new NetworkEdgeResponse
                {
                    From = e.From,
                    To = e.To,
                    Reason = e.Reason
                }));
                response.ClusterCount = analysis.Clusters.Count;

                return response;
            }
        }

        private static HashSet<Guid> ConnectedPersonIds(
            List<PersonAffinity> affinities, int maxSharedAttributeMembers)
        {
            var connected = new HashSet<Guid>();
            var byCircle = new Dictionary<string, List<Guid>>(StringComparer.OrdinalIgnoreCase);
            var byTag = new Dictionary<string, List<Guid>>(StringComparer.OrdinalIgnoreCase);
            var byChannel = new Dictionary<string, List<Guid>>(StringComparer.OrdinalIgnoreCase);

            foreach (var affinity in affinities)
            {
                foreach (var circle in affinity.CircleNames.Where(c => !string.IsNullOrWhiteSpace(c)))
                    AddToIndex(byCircle, circle.Trim(), affinity.PersonId);
                foreach (var tag in affinity.TagNames.Where(t => !string.IsNullOrWhiteSpace(t)))
                    AddToIndex(byTag, tag.Trim(), affinity.PersonId);
                foreach (var channel in affinity.ChannelNames.Where(c => !string.IsNullOrWhiteSpace(c)))
                    AddToIndex(byChannel, channel.Trim(), affinity.PersonId);
            }

            foreach (var index in new[] { byCircle, byTag, byChannel })
                foreach (var members in index.Values)
                    if (members.Count > 1 && members.Count <= maxSharedAttributeMembers)
                        foreach (var id in members)
                            connected.Add(id);

            return connected;
        }

        private static void AddToIndex(
            Dictionary<string, List<Guid>> index, string key, Guid personId)
        {
            if (!index.TryGetValue(key, out var members))
            {
                members = new List<Guid>();
                index[key] = members;
            }
            if (!members.Contains(personId))
                members.Add(personId);
        }
    }
}
