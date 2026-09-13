using ContactsManger.Core.DTOs.PersonDTOs;
using Entities;
using FluentAssertions;
using Moq;
using RepositryContracts;
using RelationshipIntelligence.AI;
using ServiceContracts;
using ServiceContracts.DTOs;
using ServiceContracts.DTOs.AgentDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CRUDTests
{
    public class StubCopilotAgentTests
    {
        private readonly Guid _userA = Guid.NewGuid();
        private readonly Guid _salmaId = Guid.NewGuid();
        private readonly Mock<IAgentSessionStore> _sessionsMock = new();
        private readonly Mock<ICurrentUserService> _userMock = new();
        private readonly Mock<IRelationshipScoringService> _scoringMock = new();
        private readonly Mock<IInteractionService> _interactionsMock = new();
        private readonly Mock<IRelationshipMemoryService> _memoryMock = new();
        private readonly Mock<IEventService> _eventsMock = new();
        private readonly Mock<INetworkAnalysisService> _networkMock = new();
        private readonly Mock<IPersonSearcherService> _searcherMock = new();
        private readonly Mock<IPersonGetterService> _personsMock = new();
        private readonly Mock<IOutreachService> _outreachMock = new();

        private readonly Dictionary<Guid, AgentSession> _store = new();

        private StubCopilotAgent Agent()
        {
            _userMock.Setup(u => u.UserId).Returns(_userA);
            _sessionsMock.Setup(s => s.GetOrCreateAsync(It.IsAny<Guid?>(), _userA))
                .ReturnsAsync((Guid? id, Guid owner) =>
                {
                    if (id != null && _store.TryGetValue(id.Value, out var existing))
                        return existing;
                    var fresh = new AgentSession { SessionId = Guid.NewGuid(), UpdatedAtUtc = DateTime.UtcNow };
                    _store[fresh.SessionId] = fresh;
                    return fresh;
                });
            _sessionsMock.Setup(s => s.SaveAsync(_userA, It.IsAny<AgentSession>()))
                .Callback<Guid, AgentSession>((owner, s) => _store[s.SessionId] = s)
                .Returns(Task.CompletedTask);
            return new StubCopilotAgent(
                _sessionsMock.Object,
                _userMock.Object,
                _scoringMock.Object,
                _interactionsMock.Object,
                _memoryMock.Object,
                _eventsMock.Object,
                _networkMock.Object,
                _searcherMock.Object,
                _personsMock.Object,
                _outreachMock.Object);
        }

        private void ArrangePeople()
        {
            _personsMock.Setup(p => p.GetAllPersons()).ReturnsAsync(new List<PersonRespones>
            {
                new() { PersonId = _salmaId, Name = "Salma El-Sayed" }
            });
            _personsMock.Setup(p => p.GetPersonByPersonId(_salmaId)).ReturnsAsync(
                new PersonRespones { PersonId = _salmaId, Name = "Salma El-Sayed" });
            _searcherMock.Setup(s => s.SearchPersonsBy_Batched("Salma El-Sayed", "Name", 1, 10))
                .ReturnsAsync(new PagedResult<PersonViewDTO>
                {
                    Items = new List<PersonViewDTO>
                    {
                        new() { PersonId = _salmaId, Name = "Salma El-Sayed" }
                    },
                    TotalCount = 1
                });
            _searcherMock.Setup(s => s.SearchPersonsBy_Batched(It.Is<string>(q => q != "Salma El-Sayed"), "Name", 1, 10))
                .ReturnsAsync(new PagedResult<PersonViewDTO> { Items = new List<PersonViewDTO>(), TotalCount = 0 });
            _interactionsMock.Setup(i => i.ListForPersonAsync(It.IsAny<Guid?>()))
                .ReturnsAsync(new List<ServiceContracts.DTOs.InteractionResponse>());
            _memoryMock.Setup(m => m.ListForPersonAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<ServiceContracts.DTOs.MemoryDTOs.MemoryEntryResponse>());
            _eventsMock.Setup(e => e.GetUpcomingAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<ServiceContracts.DTOs.EventDTOs.EventOccurrenceDto>());
            _scoringMock.Setup(s => s.GetQueueAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<RelationshipHealthResponse>());
        }

        [Fact]
        public async Task Chat_Greeting_ReturnsCapabilitiesWithoutTools()
        {
            ArrangePeople();

            var response = await Agent().ChatAsync(new AgentChatRequest { Message = "Hi" });

            response.Text.Should().Contain("keep track");
            response.SessionId.Should().NotBeEmpty();
            _scoringMock.Verify(s => s.GetQueueAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task Chat_PersonName_ResolvesAndAnswersWithFollowUpContext()
        {
            ArrangePeople();
            _interactionsMock.Setup(i => i.ListForPersonAsync(_salmaId)).ReturnsAsync(
                new List<ServiceContracts.DTOs.InteractionResponse>
                {
                    new() { InteractionId = Guid.NewGuid(), PersonId = _salmaId, InteractionType = ContactsManger.Core.Domain.Entities.EEnums.EnInteractionType.Call, InteractionTitle = "Design crit", TimeOfInteraction = DateTime.UtcNow.AddDays(-9) }
                });

            var agent = Agent();
            var first = await agent.ChatAsync(new AgentChatRequest { Message = "What happened with Salma El-Sayed?" });
            first.Text.Should().Contain("Salma El-Sayed was last in touch 9d ago");
            first.Citations.Should().ContainSingle(c => c.Label == "Salma El-Sayed");

            var second = await agent.ChatAsync(new AgentChatRequest
            {
                SessionId = first.SessionId,
                Message = "Why does that matter?",
                History = new List<ServiceContracts.DTOs.CopilotDTOs.ChatTurnDto>()
            });
            second.Text.Should().Contain("Salma El-Sayed");
        }

        [Fact]
        public async Task Chat_DirectionChange_ReplacesTask()
        {
            ArrangePeople();
            var agent = Agent();

            var first = await agent.ChatAsync(new AgentChatRequest { Message = "Prepare a meeting for tomorrow." });
            first.Text.Should().Contain("Who are you meeting");
            first.NeedsInput.Should().NotBeNull();

            var second = await agent.ChatAsync(new AgentChatRequest
            {
                SessionId = first.SessionId,
                Message = "Actually forget it, who needs attention?"
            });
            second.Text.Should().Contain("within its natural rhythm");
        }

        [Fact]
        public async Task Chat_UnknownPerson_SaysSoInsteadOfInventing()
        {
            ArrangePeople();

            var response = await Agent().ChatAsync(new AgentChatRequest { Message = "Tell me about my relationship with Ahmed." });

            response.Text.Should().Contain("I don't have a contact matching");
        }

        [Fact]
        public async Task Chat_OutreachFlow_BuildsSelectionThenApproves()
        {
            ArrangePeople();
            _scoringMock.Setup(s => s.GetQueueAsync(It.IsAny<int>())).ReturnsAsync(new List<RelationshipHealthResponse>
            {
                new() { PersonId = _salmaId, Name = "Salma El-Sayed", UrgencyScore = 74, Band = "AtRisk", InteractionCount = 7, LastContactAtUtc = DateTime.UtcNow.AddDays(-9), CadenceReferenceDays = 30, EvidenceStatus = "Established" }
            });
            var batchId = Guid.NewGuid();
            _outreachMock.Setup(o => o.BuildFromSignalsAsync(It.IsAny<ServiceContracts.DTOs.OutreachDTOs.BuildBatchFromSignalsRequest>()))
                .ReturnsAsync(new ServiceContracts.DTOs.OutreachDTOs.OutreachBatchResponse
                {
                    OutreachBatchId = batchId,
                    Intent = "Reconnect",
                    Members = new List<ServiceContracts.DTOs.OutreachDTOs.OutreachBatchMemberDto>
                    {
                        new() { OutreachBatchMemberId = Guid.NewGuid(), PersonId = _salmaId, PersonName = "Salma El-Sayed", Reason = "AtRisk" }
                    },
                    Drafts = new List<ServiceContracts.DTOs.OutreachDTOs.CommunicationDraftDto>()
                });
            _outreachMock.Setup(o => o.GetAsync(batchId)).ReturnsAsync(new ServiceContracts.DTOs.OutreachDTOs.OutreachBatchResponse
            {
                OutreachBatchId = batchId,
                Intent = "Reconnect",
                Members = new List<ServiceContracts.DTOs.OutreachDTOs.OutreachBatchMemberDto>
                {
                    new() { OutreachBatchMemberId = Guid.NewGuid(), PersonId = _salmaId, PersonName = "Salma El-Sayed", Reason = "AtRisk" }
                },
                Drafts = new List<ServiceContracts.DTOs.OutreachDTOs.CommunicationDraftDto>
                {
                    new() { CommunicationDraftId = Guid.NewGuid(), PersonId = _salmaId, PersonName = "Salma El-Sayed", Channel = OutreachChannel.Email, Body = "Hi Salma", Status = DraftStatus.Draft }
                }
            });
            _outreachMock.Setup(o => o.ApproveAsync(batchId, null)).ReturnsAsync(new ServiceContracts.DTOs.OutreachDTOs.OutreachBatchResponse
            {
                OutreachBatchId = batchId,
                Status = BatchStatus.Approved,
                Members = new List<ServiceContracts.DTOs.OutreachDTOs.OutreachBatchMemberDto>(),
                Drafts = new List<ServiceContracts.DTOs.OutreachDTOs.CommunicationDraftDto>()
            });

            var agent = Agent();
            var built = await agent.ChatAsync(new AgentChatRequest { Message = "Find everyone I should reconnect with this week." });
            built.Text.Should().Contain("Salma El-Sayed");
            built.WorkingState.Should().NotBeNull();
            built.WorkingState!.SelectedPeople.Should().Contain("Salma El-Sayed");

            var ready = await agent.ChatAsync(new AgentChatRequest { SessionId = built.SessionId, Message = "Approve." });
            ready.Text.Should().Contain("Ready to approve 1 draft");

            var approved = await agent.ChatAsync(new AgentChatRequest { SessionId = built.SessionId, Message = "Approve now." });
            approved.Text.Should().Contain("Approved");
            _outreachMock.Verify(o => o.ApproveAsync(batchId, null), Times.Once);
        }
    }
}
