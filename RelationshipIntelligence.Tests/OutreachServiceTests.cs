using Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RepositryContracts;
using RelationshipIntelligence.AI;
using ServiceContracts;
using ServiceContracts.DTOs;
using ServiceContracts.DTOs.OutreachDTOs;
using Servicess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CRUDTests
{
    public class OutreachServiceTests
    {
        private readonly Guid _userA = Guid.NewGuid();
        private readonly Mock<OutreachRepositoryContract> _batchesMock = new();
        private readonly Mock<PersonRepositryContract> _personsMock = new();
        private readonly Mock<IRelationshipScoringService> _scoringMock = new();
        private readonly Mock<IInteractionService> _interactionsMock = new();
        private readonly Mock<IRelationshipMemoryService> _memoryMock = new();
        private readonly Mock<IEventService> _eventsMock = new();
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<ICurrentUserService> _userMock = new();

        private readonly List<OutreachBatch> _store = new();
        private readonly Guid _salmaId = Guid.NewGuid();
        private readonly Guid _karimId = Guid.NewGuid();

        private OutreachService Service()
        {
            var copilot = new StubCopilotService(
                _scoringMock.Object,
                _interactionsMock.Object,
                _memoryMock.Object,
                _eventsMock.Object,
                Mock.Of<IPersonGetterService>());
            return new OutreachService(
                _batchesMock.Object,
                _personsMock.Object,
                _scoringMock.Object,
                _interactionsMock.Object,
                _memoryMock.Object,
                _eventsMock.Object,
                copilot,
                _uowMock.Object,
                _userMock.Object,
                Mock.Of<ILogger<OutreachService>>());
        }

        private void Arrange()
        {
            _userMock.Setup(u => u.UserId).Returns(_userA);
            _batchesMock.Setup(r => r.AddBatchAsync(It.IsAny<OutreachBatch>()))
                .Callback<OutreachBatch>(b => _store.Add(b))
                .Returns(Task.CompletedTask);
            _batchesMock.Setup(r => r.GetBatchAsync(_userA, It.IsAny<Guid>()))
                .ReturnsAsync((Guid owner, Guid id) => _store.FirstOrDefault(b => b.OutreachBatchId == id));
            _batchesMock.Setup(r => r.ListBatchesAsync(_userA)).ReturnsAsync(_store);
            _batchesMock.Setup(r => r.ListRecentBatchesAsync(_userA, It.IsAny<DateTime>())).ReturnsAsync(_store);
            _batchesMock.Setup(r => r.AddDraftAsync(It.IsAny<CommunicationDraft>()))
                .Callback<CommunicationDraft>(d => _store.FirstOrDefault(b => b.OutreachBatchId == d.OutreachBatchId)?.Drafts.Add(d))
                .Returns(Task.CompletedTask);

            _personsMock.Setup(r => r.GetPersonById(_salmaId)).ReturnsAsync(
                new Person { PersonId = _salmaId, ApplicationUserId = _userA, Name = "Salma El-Sayed" });
            _personsMock.Setup(r => r.GetPersonById(_karimId)).ReturnsAsync(
                new Person { PersonId = _karimId, ApplicationUserId = _userA, Name = "Karim Naguib" });
            _personsMock.Setup(r => r.GetAllPersons()).ReturnsAsync(new List<Person>
            {
                new() { PersonId = _salmaId, ApplicationUserId = _userA, Name = "Salma El-Sayed" },
                new() { PersonId = _karimId, ApplicationUserId = _userA, Name = "Karim Naguib" }
            }.AsEnumerable());

            _scoringMock.Setup(s => s.GetQueueAsync(It.IsAny<int>())).ReturnsAsync(new List<RelationshipHealthResponse>
            {
                new() { PersonId = _salmaId, Name = "Salma El-Sayed", UrgencyScore = 74, Band = "AtRisk", InteractionCount = 7, LastContactAtUtc = DateTime.UtcNow.AddDays(-9), CadenceReferenceDays = 30, EvidenceStatus = "Established" },
                new() { PersonId = _karimId, Name = "Karim Naguib", UrgencyScore = 48, Band = "Drifting", InteractionCount = 4, LastContactAtUtc = DateTime.UtcNow.AddDays(-12), CadenceReferenceDays = 14, EvidenceStatus = "Established" }
            });
            _interactionsMock.Setup(i => i.ListForPersonAsync(It.IsAny<Guid?>()))
                .ReturnsAsync(new List<ServiceContracts.DTOs.InteractionResponse>());
            _interactionsMock.Setup(i => i.ListForPersonAsync(_salmaId)).ReturnsAsync(new List<ServiceContracts.DTOs.InteractionResponse>
            {
                new() { InteractionId = Guid.NewGuid(), PersonId = _salmaId, InteractionType = ContactsManger.Core.Domain.Entities.EEnums.EnInteractionType.Call, InteractionTitle = "Design crit", TimeOfInteraction = DateTime.UtcNow.AddDays(-9) }
            });
            _memoryMock.Setup(m => m.ListForPersonAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<ServiceContracts.DTOs.MemoryDTOs.MemoryEntryResponse>());
            _memoryMock.Setup(m => m.ListForPersonAsync(_salmaId)).ReturnsAsync(new List<ServiceContracts.DTOs.MemoryDTOs.MemoryEntryResponse>
            {
                new() { MemoryEntryId = Guid.NewGuid(), PersonId = _salmaId, Kind = RelationshipMemoryKind.Commitment, Title = "Send crit notes", Status = MemoryEntryStatus.Active, Provenance = MemoryProvenance.User }
            });
            _eventsMock.Setup(e => e.GetUpcomingAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<ServiceContracts.DTOs.EventDTOs.EventOccurrenceDto>());
        }

        [Fact]
        public async Task BuildFromSignals_AttentionQueue_CreatesMembersWithReasons()
        {
            Arrange();

            var batch = await Service().BuildFromSignalsAsync(new BuildBatchFromSignalsRequest
            {
                SignalFilters = new List<string> { "attentionQueue" },
                Intent = "Reconnect"
            });

            batch.Members.Should().HaveCount(2);
            batch.Members.Should().OnlyContain(m => !string.IsNullOrWhiteSpace(m.Reason));
            batch.Members.First(m => m.PersonId == _salmaId).Reason.Should().Contain("AtRisk");
        }

        [Fact]
        public async Task BuildFromSignals_UnknownSignal_Throws()
        {
            Arrange();

            await Assert.ThrowsAsync<ArgumentException>(() => Service().BuildFromSignalsAsync(
                new BuildBatchFromSignalsRequest { SignalFilters = new List<string> { "telepathy" } }));
        }

        [Fact]
        public async Task GenerateDrafts_SameInstruction_GroundsEachDraftInOwnContext()
        {
            Arrange();
            var batch = await Service().BuildFromSignalsAsync(new BuildBatchFromSignalsRequest
            {
                SignalFilters = new List<string> { "attentionQueue" },
                Intent = "Reconnect"
            });
            await Service().UpdateSettingsAsync(batch.OutreachBatchId, new BatchSettingsRequest
            {
                Channel = OutreachChannel.Email,
                Intent = "Reconnect",
                GlobalInstruction = "Keep it casual and short."
            });

            var result = await Service().GenerateDraftsAsync(batch.OutreachBatchId);

            result.Drafts.Should().HaveCount(2);
            var salma = result.Drafts.Single(d => d.PersonId == _salmaId);
            var karim = result.Drafts.Single(d => d.PersonId == _karimId);
            salma.ContextUsed.Should().Contain("crit");
            karim.ContextUsed.Should().NotContain("crit");
            salma.Body.Should().Contain("Salma");
            karim.Body.Should().NotContain("Salma");
            result.Drafts.Should().OnlyContain(d => d.Status == DraftStatus.Draft);
        }

        [Fact]
        public async Task GenerateDrafts_ChannelOverride_WinsOverBatchDefault()
        {
            Arrange();
            var batch = await Service().BuildFromSignalsAsync(new BuildBatchFromSignalsRequest
            {
                SignalFilters = new List<string> { "attentionQueue" },
                Intent = "Reconnect"
            });
            var member = batch.Members.First(m => m.PersonId == _karimId);
            await Service().UpdateMemberAsync(batch.OutreachBatchId, member.OutreachBatchMemberId, new MemberOverrideRequest
            {
                ChannelOverride = OutreachChannel.LinkedIn
            });

            var result = await Service().GenerateDraftsAsync(batch.OutreachBatchId);

            result.Drafts.Single(d => d.PersonId == _karimId).Channel.Should().Be(OutreachChannel.LinkedIn);
            result.Drafts.Single(d => d.PersonId == _salmaId).Channel.Should().Be(OutreachChannel.Email);
        }

        [Fact]
        public async Task GenerateDrafts_CallPrep_ProducesThreeSections()
        {
            Arrange();
            var batch = await Service().BuildFromSignalsAsync(new BuildBatchFromSignalsRequest
            {
                SignalFilters = new List<string> { "attentionQueue" },
                Intent = "Prepare calls"
            });
            await Service().UpdateSettingsAsync(batch.OutreachBatchId, new BatchSettingsRequest
            {
                Channel = OutreachChannel.CallPrep,
                Intent = "Prepare calls"
            });

            var result = await Service().GenerateDraftsAsync(batch.OutreachBatchId);

            result.Drafts.Should().OnlyContain(d => d.Kind == DraftKind.CallPrep);
            foreach (var draft in result.Drafts)
            {
                draft.Body.Should().Contain("Before");
                draft.Body.Should().Contain("During");
                draft.Body.Should().Contain("After");
            }
        }

        [Fact]
        public async Task Approve_MarksDraftsButRecordsNoInteraction()
        {
            Arrange();
            var batch = await Service().BuildFromSignalsAsync(new BuildBatchFromSignalsRequest
            {
                SignalFilters = new List<string> { "attentionQueue" },
                Intent = "Reconnect"
            });
            await Service().GenerateDraftsAsync(batch.OutreachBatchId);

            var approved = await Service().ApproveAsync(batch.OutreachBatchId, null);

            approved.Status.Should().Be(BatchStatus.Approved);
            approved.Drafts.Should().OnlyContain(d => d.Status == DraftStatus.Approved);
            _interactionsMock.Verify(i => i.LogAsync(It.IsAny<ServiceContracts.DTOs.InteractionAddRequest>()), Times.Never);
        }

        [Fact]
        public async Task Approve_EmptyReviewable_Throws()
        {
            Arrange();
            var batch = await Service().BuildFromSignalsAsync(new BuildBatchFromSignalsRequest
            {
                SignalFilters = new List<string> { "attentionQueue" },
                Intent = "Reconnect"
            });

            await Assert.ThrowsAsync<InvalidOperationException>(() => Service().ApproveAsync(batch.OutreachBatchId, null));
        }

        [Fact]
        public async Task ReviewDraft_UserEdit_PreservesOriginalAndSuggestsStyle()
        {
            // AI draft → user edit history preserved; edit becomes a
            // SUGGESTION for the user to approve, never a silent rule.
            Arrange();
            _memoryMock.Setup(m => m.SuggestEntryAsync(
                    It.IsAny<Guid>(), It.IsAny<RelationshipMemoryKind>(),
                    It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new ServiceContracts.DTOs.MemoryDTOs.MemoryEntryResponse
                {
                    MemoryEntryId = Guid.NewGuid(), Kind = RelationshipMemoryKind.Preference,
                    Title = "Style: prefers shorter messages than drafted",
                    Status = MemoryEntryStatus.Active, Provenance = MemoryProvenance.AiSuggested
                });
            var batch = await Service().BuildFromSignalsAsync(new BuildBatchFromSignalsRequest
            {
                SignalFilters = new List<string> { "attentionQueue" },
                Intent = "Reconnect"
            });
            var generated = await Service().GenerateDraftsAsync(batch.OutreachBatchId);
            var salma = generated.Drafts.Single(d => d.PersonId == _salmaId);
            var aiBody = salma.Body;
            aiBody.Should().StartWith("Hi ");

            var edited = await Service().ReviewDraftAsync(salma.CommunicationDraftId, new DraftReviewRequest
            {
                Status = DraftStatus.Edited,
                Body = "Sending the crit notes now — let me know what you think."
            });

            edited.Status.Should().Be(DraftStatus.Edited);
            edited.IsAiGenerated.Should().BeFalse();
            edited.OriginalBody.Should().Be(aiBody);
            _memoryMock.Verify(m => m.SuggestEntryAsync(
                _salmaId, RelationshipMemoryKind.Preference,
                It.Is<string>(t => t.StartsWith("Style:")), It.IsAny<string>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task ReviewDraft_SecondEdit_KeepsFirstOriginal()
        {
            Arrange();
            _memoryMock.Setup(m => m.SuggestEntryAsync(
                    It.IsAny<Guid>(), It.IsAny<RelationshipMemoryKind>(),
                    It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new ServiceContracts.DTOs.MemoryDTOs.MemoryEntryResponse
                {
                    MemoryEntryId = Guid.NewGuid(), Kind = RelationshipMemoryKind.Preference,
                    Title = "Style: prefers shorter messages than drafted",
                    Status = MemoryEntryStatus.Active, Provenance = MemoryProvenance.AiSuggested
                });
            var batch = await Service().BuildFromSignalsAsync(new BuildBatchFromSignalsRequest
            {
                SignalFilters = new List<string> { "attentionQueue" },
                Intent = "Reconnect"
            });
            var generated = await Service().GenerateDraftsAsync(batch.OutreachBatchId);
            var salma = generated.Drafts.Single(d => d.PersonId == _salmaId);
            var aiBody = salma.Body;

            await Service().ReviewDraftAsync(salma.CommunicationDraftId, new DraftReviewRequest
            {
                Status = DraftStatus.Edited, Body = "Sending the crit notes now."
            });
            var twice = await Service().ReviewDraftAsync(salma.CommunicationDraftId, new DraftReviewRequest
            {
                Status = DraftStatus.Edited, Body = "Crit notes sent, let me know."
            });

            twice.OriginalBody.Should().Be(aiBody);
        }
    }
}
