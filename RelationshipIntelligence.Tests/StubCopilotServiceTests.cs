using Entities;
using FluentAssertions;
using Moq;
using RepositryContracts;
using RelationshipIntelligence.AI;
using ServiceContracts;
using ServiceContracts.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CRUDTests
{
    public class StubCopilotServiceTests
    {
        private readonly Guid _userA = Guid.NewGuid();
        private readonly Guid _salmaId = Guid.NewGuid();
        private readonly Guid _karimId = Guid.NewGuid();
        private readonly Mock<IRelationshipScoringService> _scoringMock = new();
        private readonly Mock<IInteractionService> _interactionsMock = new();
        private readonly Mock<IRelationshipMemoryService> _memoryMock = new();
        private readonly Mock<IEventService> _eventsMock = new();
        private readonly Mock<IPersonGetterService> _personsMock = new();

        private StubCopilotService Service() => new(
            _scoringMock.Object,
            _interactionsMock.Object,
            _memoryMock.Object,
            _eventsMock.Object,
            _personsMock.Object);

        private void ArrangeQueue()
        {
            _scoringMock.Setup(s => s.GetQueueAsync(It.IsAny<int>())).ReturnsAsync(new List<RelationshipHealthResponse>
            {
                new() { PersonId = _salmaId, Name = "Salma El-Sayed", UrgencyScore = 74, Band = "AtRisk", InteractionCount = 7 },
                new() { PersonId = _karimId, Name = "Karim Naguib", UrgencyScore = 48, Band = "Drifting", InteractionCount = 4 }
            });
            _eventsMock.Setup(e => e.GetUpcomingAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<ServiceContracts.DTOs.EventDTOs.EventOccurrenceDto>());
            _interactionsMock.Setup(i => i.ListForPersonAsync(It.IsAny<Guid?>()))
                .ReturnsAsync(new List<ServiceContracts.DTOs.InteractionResponse>());
            _memoryMock.Setup(m => m.ListForPersonAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<ServiceContracts.DTOs.MemoryDTOs.MemoryEntryResponse>());
            _personsMock.Setup(p => p.GetPersonByPersonId(_salmaId)).ReturnsAsync(
                new PersonRespones { PersonId = _salmaId, Name = "Salma El-Sayed" });
        }

        [Fact]
        public async Task BuildBriefing_AttentionIsSubsetOfDeterministicQueue()
        {
            ArrangeQueue();

            var briefing = await Service().BuildBriefingAsync();
            var queueIds = new List<Guid> { _salmaId, _karimId };

            briefing.AttentionNow.Should().NotBeEmpty();
            briefing.AttentionNow.Select(a => a.PersonId).Should().OnlyContain(id => queueIds.Contains(id));
            briefing.Summary.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task SuggestPlan_ReflectsIntentAndActualSignals()
        {
            ArrangeQueue();
            _memoryMock.Setup(m => m.ListForPersonAsync(_salmaId)).ReturnsAsync(
                new List<ServiceContracts.DTOs.MemoryDTOs.MemoryEntryResponse>
                {
                    new() { MemoryEntryId = Guid.NewGuid(), PersonId = _salmaId, Kind = RelationshipMemoryKind.Intent, Title = "Strengthen the relationship", Status = MemoryEntryStatus.Active, Provenance = MemoryProvenance.User }
                });

            var plan = await Service().SuggestPlanAsync(_salmaId, null);

            plan.Intent.Should().Be("Strengthen the relationship");
            plan.SuggestedActions.Should().NotBeEmpty();
            plan.SuggestedActions.First().WhyNow.Should().Contain("74");
        }

        [Fact]
        public void Match_AcceptanceUtterances_ProduceSignalsWithoutPeople()
        {
            var cases = new Dictionary<string, string[]>
            {
                ["Prepare messages for everyone I should reconnect with this week."] = new[] { "outsideCadence" },
                ["Who should I follow up with after last week's meetings?"] = new[] { "recentMeetings", "pendingCommitments" },
                ["Prepare short LinkedIn messages for the people I've neglected recently."] = new[] { "neglected" },
                ["I want to reconnect with everyone in my attention queue. Use email."] = new[] { "attentionQueue" }
            };

            foreach (var (text, expectedSignals) in cases)
            {
                var intent = OutreachIntentMatcher.Match(text);
                intent.NeedsClarification.Should().BeFalse($"for '{text}'");
                intent.SignalFilters.Should().Contain(expectedSignals);
                var json = System.Text.Json.JsonSerializer.Serialize(intent);
                json.Should().NotContain("personId");
            }

            var linked = OutreachIntentMatcher.Match("Prepare short LinkedIn messages for the people I've neglected recently.");
            linked.Channel.Should().Be("LinkedIn");
            var emailed = OutreachIntentMatcher.Match("I want to reconnect with everyone in my attention queue. Use email.");
            emailed.Channel.Should().Be("Email");
        }

        [Fact]
        public void Match_UnmappableRequest_AsksForClarification()
        {
            var intent = OutreachIntentMatcher.Match("Tell me about the weather.");
            intent.NeedsClarification.Should().BeTrue();
        }

        [Fact]
        public async Task Ask_SynthesizesNarrativeFromActualData()
        {
            ArrangeQueue();
            _personsMock.Setup(p => p.GetPersonByPersonId(_salmaId)).ReturnsAsync(
                new PersonRespones { PersonId = _salmaId, Name = "Salma El-Sayed" });
            _interactionsMock.Setup(i => i.ListForPersonAsync(_salmaId)).ReturnsAsync(
                new List<ServiceContracts.DTOs.InteractionResponse>
                {
                    new() { InteractionId = Guid.NewGuid(), PersonId = _salmaId, InteractionType = ContactsManger.Core.Domain.Entities.EEnums.EnInteractionType.Call, InteractionTitle = "Design crit", TimeOfInteraction = DateTime.UtcNow.AddDays(-9) }
                });
            _memoryMock.Setup(m => m.ListForPersonAsync(_salmaId)).ReturnsAsync(
                new List<ServiceContracts.DTOs.MemoryDTOs.MemoryEntryResponse>());

            var general = await Service().AskAsync("Who needs attention?", null, null);
            general.Text.Should().Contain("deserves attention");
            general.Text.Should().Contain("Salma El-Sayed");
            general.Text.Should().NotContain("[Observed]");

            var personal = await Service().AskAsync("What happened?", _salmaId, null);
            personal.Text.Should().Contain("Salma El-Sayed was last in touch 9d ago");
            personal.Text.Should().Contain("Design crit");
            personal.Citations.Should().ContainSingle(c => c.Label == "Salma El-Sayed");
        }
    }
}
