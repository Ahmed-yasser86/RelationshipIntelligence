using FluentAssertions;
using Moq;
using RelationshipIntelligence.AI;
using ServiceContracts;
using ServiceContracts.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace CRUDTests
{
    /// <summary>
    /// Read-only tools must never disclose data for ids outside the
    /// authenticated user's scope. Owner-scoped services return null/empty for
    /// foreign ids; tools must surface that as "not observed", never as data.
    /// </summary>
    public class RelationshipQueryPluginTests
    {
        private readonly Mock<IRelationshipScoringService> _scoringMock = new();

        private RelationshipQueryPlugin Plugin() => new(
            _scoringMock.Object,
            Mock.Of<IInteractionService>(),
            Mock.Of<IRelationshipMemoryService>(),
            Mock.Of<IEventService>(),
            Mock.Of<INetworkAnalysisService>(),
            Mock.Of<IPersonSearcherService>(),
            Mock.Of<IPersonGetterService>(),
            Mock.Of<IMeetingService>());

        [Fact]
        public async Task ExplainAttentionSignal_UnknownId_RevealsNothing()
        {
            _scoringMock.Setup(s => s.GetQueueAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<RelationshipHealthResponse>());

            var result = await Plugin().ExplainAttentionSignalAsync(Guid.NewGuid().ToString());

            result.Should().Contain("Not currently in the attention queue");
            result.Should().NotContain("urgency");
        }

        [Fact]
        public async Task GetCadenceAnalysis_UnknownId_RevealsNothing()
        {
            _scoringMock.Setup(s => s.GetQueueAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<RelationshipHealthResponse>());

            var result = await Plugin().GetCadenceAnalysisAsync(Guid.NewGuid().ToString());

            result.Should().Contain("No scored state");
        }

        [Fact]
        public async Task SearchPeople_WireFormat_MatchesAgentParser()
        {
            // The live agent reads personId/name exactly; PascalCase wire
            // output silently resolved nobody.
            var id = Guid.NewGuid();
            var searcherMock = new Mock<IPersonSearcherService>();
            searcherMock.Setup(s => s.SearchPersonsBy_Batched("Mohamed", "Name", 1, 10))
                .ReturnsAsync(new ServiceContracts.DTOs.PagedResult<ContactsManger.Core.DTOs.PersonDTOs.PersonViewDTO>
                {
                    Items = new List<ContactsManger.Core.DTOs.PersonDTOs.PersonViewDTO>
                    {
                        new() { PersonId = id, Name = "Mohamed Farouk" }
                    },
                    TotalCount = 1
                });
            var plugin = new RelationshipQueryPlugin(
                _scoringMock.Object,
                Mock.Of<IInteractionService>(),
                Mock.Of<IRelationshipMemoryService>(),
                Mock.Of<IEventService>(),
                Mock.Of<INetworkAnalysisService>(),
                searcherMock.Object,
                Mock.Of<IPersonGetterService>(),
                Mock.Of<IMeetingService>());

            var raw = await plugin.SearchPeopleAsync("Mohamed");

            using var doc = System.Text.Json.JsonDocument.Parse(raw);
            doc.RootElement.GetArrayLength().Should().Be(1);
            doc.RootElement[0].GetProperty("personId").GetGuid().Should().Be(id);
            doc.RootElement[0].GetProperty("name").GetString().Should().Be("Mohamed Farouk");
        }

        [Fact]
        public async Task MalformedId_ReturnsErrorWithoutThrowing()
        {
            var result = await Plugin().ExplainAttentionSignalAsync("not-a-guid");

            result.Should().Contain("Invalid person id");
        }
    }
}
