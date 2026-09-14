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

        private readonly Mock<IDigestService> _digestMock = new();
        private readonly Mock<IMeetingService> _meetingsMock = new();
        private readonly Mock<IRelationshipMemoryService> _memoryMock = new();
        private readonly Mock<IPersonGetterService> _personsMock = new();

        private RelationshipQueryPlugin Plugin() => new(
            _scoringMock.Object,
            Mock.Of<IInteractionService>(),
            _memoryMock.Object,
            Mock.Of<IEventService>(),
            Mock.Of<INetworkAnalysisService>(),
            Mock.Of<IPersonSearcherService>(),
            _personsMock.Object,
            _meetingsMock.Object,
            _digestMock.Object,
            Mock.Of<IRelationshipPreferenceService>());

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
                Mock.Of<IMeetingService>(),
                Mock.Of<IDigestService>(),
                Mock.Of<IRelationshipPreferenceService>());

            var raw = await plugin.SearchPeopleAsync("Mohamed");

            using var doc = System.Text.Json.JsonDocument.Parse(raw);
            doc.RootElement.GetArrayLength().Should().Be(1);
            doc.RootElement[0].GetProperty("personId").GetGuid().Should().Be(id);
            doc.RootElement[0].GetProperty("name").GetString().Should().Be("Mohamed Farouk");
        }

        [Fact]
        public void ProviderRejection_NamesStatusAndPointsAtSetup()
        {
            var rejected = new Microsoft.SemanticKernel.HttpOperationException("Service request failed.")
            {
                StatusCode = System.Net.HttpStatusCode.BadRequest
            };
            var ex = RelationshipIntelligence.AI.CopilotErrors.FromModelFailure(rejected);

            ex.Message.Should().Contain("BadRequest");
            ex.Message.Should().Contain("Setup");
        }

        [Fact]
        public void RateLimit_SaysToWaitAndRetry()
        {
            var limited = new Microsoft.SemanticKernel.HttpOperationException("Service request failed.")
            {
                StatusCode = System.Net.HttpStatusCode.TooManyRequests
            };
            var ex = RelationshipIntelligence.AI.CopilotErrors.FromModelFailure(limited);

            ex.Message.Should().Contain("rate-limiting");
        }

        [Fact]
        public void ConnectivityFailure_StaysAReachabilityProblem()
        {
            var ex = RelationshipIntelligence.AI.CopilotErrors.FromModelFailure(
                new System.Net.Http.HttpRequestException("No such host."));

            ex.Message.Should().Contain("could not reach");
        }

        [Fact]
        public async Task MalformedId_ReturnsErrorWithoutThrowing()
        {
            var result = await Plugin().ExplainAttentionSignalAsync("not-a-guid");

            result.Should().Contain("Invalid person id");
        }

        [Fact]
        public async Task GetDigest_ReturnsEntriesWithSuggestions()
        {
            var personId = Guid.NewGuid();
            _digestMock.Setup(d => d.BuildAsync(It.IsAny<string>()))
                .ReturnsAsync(new ServiceContracts.DTOs.DigestPayload
                {
                    WeekStartUtc = DateTime.UtcNow,
                    NetworkHealth = 61.5,
                    Entries = new List<ServiceContracts.DTOs.DigestEntry>
                    {
                        new()
                        {
                            Health = new RelationshipHealthResponse
                            {
                                PersonId = personId, Name = "Salma El-Sayed",
                                Band = "AtRisk", UrgencyScore = 74
                            },
                            Suggestion = "Ask how the crit notes progressed."
                        }
                    }
                });

            var result = await Plugin().GetDigestAsync();

            result.Should().Contain("Salma El-Sayed");
            result.Should().Contain("Ask how the crit notes progressed.");
        }

        [Fact]
        public async Task GetPendingReview_SurfacesUnapprovedProposalsOnly()
        {
            var personId = Guid.NewGuid();
            _personsMock.Setup(p => p.GetAllPersons()).ReturnsAsync(new List<ServiceContracts.DTOs.PersonRespones>
            {
                new() { PersonId = personId, Name = "Salma El-Sayed" }
            });
            _memoryMock.Setup(m => m.ListForPersonAsync(personId)).ReturnsAsync(
                new List<ServiceContracts.DTOs.MemoryDTOs.MemoryEntryResponse>
                {
                    new() { MemoryEntryId = Guid.NewGuid(), PersonId = personId, Kind = Entities.RelationshipMemoryKind.Topic, Title = "Suggested topic", Status = Entities.MemoryEntryStatus.Active, Provenance = Entities.MemoryProvenance.AiSuggested },
                    new() { MemoryEntryId = Guid.NewGuid(), PersonId = personId, Kind = Entities.RelationshipMemoryKind.Fact, Title = "Confirmed fact", Status = Entities.MemoryEntryStatus.Active, Provenance = Entities.MemoryProvenance.User }
                });
            _meetingsMock.Setup(m => m.ListAsync()).ReturnsAsync(new List<ServiceContracts.DTOs.MeetingDTOs.MeetingResponse>());

            var result = await Plugin().GetPendingReviewAsync();

            result.Should().Contain("Suggested topic");
            result.Should().NotContain("Confirmed fact");
        }
    }
}
