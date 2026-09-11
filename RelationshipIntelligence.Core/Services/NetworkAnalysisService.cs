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

                var persons = (await _persons.GetAllPersons())
                    .Where(p => p != null)
                    .ToList();
                var states = (await _states.ListForOwnerAsync(userId.Value))
                    .ToDictionary(s => s.PersonId);

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
                        UrgencyScore = states.TryGetValue(person.PersonId, out var s) ? s.UrgencyScore : 0
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
    }
}
