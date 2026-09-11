using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RepositryContracts;
using ServiceContracts;
using Servicess;
using Xunit;

namespace CRUDTests
{
    public class NetworkAnalyzerTests
    {
        private static GraphNodeInput Node(string? circle, string? tags = null, string? channel = null) =>
            new(Guid.NewGuid(), circle, tags, channel);

        [Fact]
        public void Analyze_TwoClustersWithBridge_FlagsBridge()
        {
            var bridge = new GraphNodeInput(Guid.NewGuid(), null, null, null);
            var a1 = new GraphNodeInput(Guid.NewGuid(), "Alpha", null, null);
            var a2 = new GraphNodeInput(Guid.NewGuid(), "Alpha", null, null);
            var b1 = new GraphNodeInput(Guid.NewGuid(), "Beta", null, null);

            var nodes = new List<GraphNodeInput> { a1, a2, bridge, b1 };
            var groups = new List<IReadOnlyList<Guid>>
            {
                new List<Guid> { a1.PersonId, a2.PersonId, bridge.PersonId },
                new List<Guid> { bridge.PersonId, b1.PersonId }
            };

            var result = NetworkAnalyzer.Analyze(nodes, groups);

            result.Bridges.Should().Contain(bridge.PersonId);
            result.Bridges.Should().NotContain(a1.PersonId);
            result.Clusters.Should().ContainSingle();
            result.Degrees[bridge.PersonId].Should().Be(3);
        }

        [Fact]
        public void Analyze_DisconnectedClusters_ListsSeparately()
        {
            var nodes = new List<GraphNodeInput>
            {
                new(Guid.NewGuid(), "Alpha", null, null),
                new(Guid.NewGuid(), "Alpha", null, null),
                new(Guid.NewGuid(), "Beta", null, null)
            };

            var result = NetworkAnalyzer.Analyze(nodes, new List<IReadOnlyList<Guid>>());

            result.Clusters.Should().HaveCount(2);
            result.Bridges.Should().BeEmpty();
        }

        [Fact]
        public void Analyze_NoSharedAttributes_NoEdgesNoCrash()
        {
            var nodes = new List<GraphNodeInput>
            {
                new(Guid.NewGuid(), "A", null, null),
                new(Guid.NewGuid(), "B", null, null)
            };

            var result = NetworkAnalyzer.Analyze(nodes, new List<IReadOnlyList<Guid>>());

            result.Edges.Should().BeEmpty();
            result.Degrees.Values.Should().AllBeEquivalentTo(0);
        }

        [Fact]
        public void Analyze_SharedTags_CreateEdge()
        {
            var nodes = new List<GraphNodeInput>
            {
                Node("A", "[\"fintech\"]"),
                Node("B", "[\"fintech\"]")
            };

            var result = NetworkAnalyzer.Analyze(nodes, new List<IReadOnlyList<Guid>>());

            result.Edges.Should().ContainSingle();
            result.Edges[0].Reason.Should().Contain("tags");
        }

        [Fact]
        public void Analyze_InvalidTagsJson_Ignored()
        {
            var nodes = new List<GraphNodeInput>
            {
                Node("A", "not-json{{{"),
                Node("B", "[\"x\"]")
            };

            Action act = () => NetworkAnalyzer.Analyze(nodes, new List<IReadOnlyList<Guid>>());
            act.Should().NotThrow();
        }

        [Fact]
        public async Task Service_ScopesGraphToOwner()
        {
            var userA = Guid.NewGuid();
            var userMock = new Mock<ICurrentUserService>();
            userMock.Setup(u => u.UserId).Returns(userA);

            var personsMock = new Mock<PersonRepositryContract>();
            personsMock.Setup(r => r.GetAllPersons()).ReturnsAsync(new List<Person>
            {
                new() { PersonId = Guid.NewGuid(), ApplicationUserId = userA, Name = "A" },
                new() { PersonId = Guid.NewGuid(), ApplicationUserId = userA, Name = "B" }
            }.AsEnumerable());

            var statesMock = new Mock<RelationshipStateRepositoryContract>();
            statesMock.Setup(r => r.ListForOwnerAsync(userA)).ReturnsAsync(new List<RelationshipState>());

            var service = new NetworkAnalysisService(
                personsMock.Object, statesMock.Object, userMock.Object,
                Mock.Of<IUnitOfWork>(), Mock.Of<ILogger<NetworkAnalysisService>>());

            var graph = await service.GetGraphAsync();

            graph.Nodes.Should().HaveCount(2);
            personsMock.Verify(r => r.GetAllPersons(), Times.Once);
            statesMock.Verify(r => r.ListForOwnerAsync(userA), Times.Once);
        }
    }
}
