using Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RepositryContracts;
using ServiceContracts;
using ServiceContracts.DTOs;
using ServiceContracts.DTOs.MeetingDTOs;
using Servicess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CRUDTests
{
    public class MeetingServiceTests
    {
        private readonly Guid _userA = Guid.NewGuid();
        private readonly Mock<MeetingRepositoryContract> _meetingsMock = new();
        private readonly Mock<RelationshipMemoryRepositoryContract> _memoryRepoMock = new();
        private readonly Mock<PersonRepositryContract> _personsMock = new();
        private readonly Mock<IInteractionService> _interactionsMock = new();
        private readonly Mock<IRelationshipMemoryService> _memoryMock = new();
        private readonly Mock<IEventService> _eventsMock = new();
        private readonly Mock<IRelationshipScoringService> _scoringMock = new();
        private readonly Mock<IMeetingExtractor> _extractorMock = new();
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<ICurrentUserService> _userMock = new();

        private readonly List<Meeting> _store = new();

        private MeetingService Service() => new(
            _meetingsMock.Object,
            _memoryRepoMock.Object,
            _personsMock.Object,
            _interactionsMock.Object,
            _memoryMock.Object,
            _eventsMock.Object,
            _scoringMock.Object,
            _extractorMock.Object,
            _uowMock.Object,
            _userMock.Object,
            Mock.Of<ILogger<MeetingService>>());

        private void ArrangeStore()
        {
            _userMock.Setup(u => u.UserId).Returns(_userA);
            _meetingsMock.Setup(r => r.AddAsync(It.IsAny<Meeting>()))
                .Callback<Meeting>(m => _store.Add(m))
                .Returns(Task.CompletedTask);
            _meetingsMock.Setup(r => r.ListAsync(_userA)).ReturnsAsync(_store);
            _meetingsMock.Setup(r => r.GetAsync(_userA, It.IsAny<Guid>()))
                .ReturnsAsync((Guid owner, Guid id) => _store.FirstOrDefault(m => m.MeetingId == id));
            // Simulate EF relationship fixup: explicitly added children appear on the parent.
            _meetingsMock.Setup(r => r.AddPersonAsync(It.IsAny<MeetingPerson>()))
                .Callback<MeetingPerson>(p => _store.FirstOrDefault(m => m.MeetingId == p.MeetingId)?.People.Add(p))
                .Returns(Task.CompletedTask);
            _meetingsMock.Setup(r => r.AddFindingAsync(It.IsAny<MeetingFinding>()))
                .Callback<MeetingFinding>(f => _store.FirstOrDefault(m => m.MeetingId == f.MeetingId)?.Findings.Add(f))
                .Returns(Task.CompletedTask);
            _meetingsMock.Setup(r => r.AddBriefAsync(It.IsAny<MeetingBrief>()))
                .Callback<MeetingBrief>(b => { var m = _store.FirstOrDefault(x => x.MeetingId == b.MeetingId); if (m != null) m.Brief = b; })
                .Returns(Task.CompletedTask);
            _personsMock.Setup(r => r.GetAllPersons()).ReturnsAsync(new List<Person>().AsEnumerable());
            _memoryMock.Setup(m => m.ListForPersonAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<ServiceContracts.DTOs.MemoryDTOs.MemoryEntryResponse>());
        }

        private Meeting CreatePrep(string title = "Planning session")
        {
            return new Meeting
            {
                MeetingId = Guid.NewGuid(),
                ApplicationUserId = _userA,
                Title = title,
                OccurredAtUtc = DateTime.UtcNow.AddDays(1),
                Status = MeetingStatus.Preparation,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                People = new List<MeetingPerson>
                {
                    new() { MeetingPersonId = Guid.NewGuid(), DetectedName = "Salma", MatchStatus = PersonMatchStatus.Unmapped }
                },
                Findings = new List<MeetingFinding>()
            };
        }

        [Fact]
        public async Task BeginLogging_PreservesIdentityParticipantsDateAndBrief()
        {
            ArrangeStore();
            var prep = CreatePrep();
            var personId = Guid.NewGuid();
            prep.People.First().MappedPersonId = personId;
            prep.People.First().MatchStatus = PersonMatchStatus.Confirmed;
            prep.Brief = new MeetingBrief
            {
                MeetingBriefId = Guid.NewGuid(),
                MeetingId = prep.MeetingId,
                Goal = "Align on scope",
                BriefJson = "{\"participants\":[]}",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            _store.Add(prep);

            var result = await Service().BeginLoggingAsync(prep.MeetingId);

            result.MeetingId.Should().Be(prep.MeetingId);
            result.Status.Should().Be(MeetingStatus.Draft);
            result.OccurredAtUtc.Should().Be(prep.OccurredAtUtc);
            result.People.Should().ContainSingle(p => p.DetectedName == "Salma" && p.MappedPersonId == personId);
            result.Brief.Should().NotBeNull();
            result.Brief!.Goal.Should().Be("Align on scope");
        }

        [Fact]
        public async Task BeginLogging_NonPreparation_Throws()
        {
            ArrangeStore();
            var draft = CreatePrep();
            draft.Status = MeetingStatus.Draft;
            _store.Add(draft);

            await Assert.ThrowsAsync<InvalidOperationException>(() => Service().BeginLoggingAsync(draft.MeetingId));
        }

        [Fact]
        public async Task Confirm_BriefOnly_CreatesNoInteractionsAndNoMemory()
        {
            ArrangeStore();
            var meeting = CreatePrep();
            meeting.Status = MeetingStatus.Processed;
            meeting.Brief = new MeetingBrief
            {
                MeetingBriefId = Guid.NewGuid(),
                MeetingId = meeting.MeetingId,
                Goal = "Discuss partnership",
                BriefJson = "{\"participants\":[]}",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            _store.Add(meeting);

            var result = await Service().ConfirmAsync(new MeetingConfirmRequest { MeetingId = meeting.MeetingId });

            result.Status.Should().Be(MeetingStatus.Confirmed);
            _interactionsMock.Verify(i => i.LogAsync(It.IsAny<InteractionAddRequest>()), Times.Never);
            _memoryRepoMock.Verify(r => r.AddAsync(It.IsAny<RelationshipMemoryEntry>()), Times.Never);
        }

        [Fact]
        public async Task Confirm_SelectedPerson_CreatesInteractionWithProvenance()
        {
            ArrangeStore();
            var personId = Guid.NewGuid();
            _personsMock.Setup(r => r.GetPersonById(personId))
                .ReturnsAsync(new Person { PersonId = personId, ApplicationUserId = _userA, Name = "Salma" });

            var meeting = CreatePrep("Partnership discussion");
            meeting.Status = MeetingStatus.Processed;
            meeting.OccurredAtUtc = DateTime.UtcNow.AddDays(-1);
            meeting.People.First().MappedPersonId = personId;
            meeting.People.First().MatchStatus = PersonMatchStatus.Confirmed;
            meeting.People.First().SelectedForLogging = true;
            var findingId = Guid.NewGuid();
            meeting.Findings.Add(new MeetingFinding
            {
                MeetingFindingId = findingId,
                MeetingId = meeting.MeetingId,
                MappedPersonId = personId,
                Kind = FindingKind.Commitment,
                Title = "Send the proposal",
                Status = FindingStatus.Accepted,
                CreatedAtUtc = DateTime.UtcNow
            });
            _store.Add(meeting);

            InteractionAddRequest? logged = null;
            _interactionsMock.Setup(i => i.LogAsync(It.IsAny<InteractionAddRequest>()))
                .Callback<InteractionAddRequest>(r => logged = r)
                .ReturnsAsync(new ServiceContracts.DTOs.InteractionResponse());

            var result = await Service().ConfirmAsync(new MeetingConfirmRequest { MeetingId = meeting.MeetingId });

            result.Status.Should().Be(MeetingStatus.Confirmed);
            logged.Should().NotBeNull();
            logged!.PersonId.Should().Be(personId);
            logged.SourceMeetingId.Should().Be(meeting.MeetingId);
            logged.InteractionTitle.Should().Be("Partnership discussion");
            logged.InteractionDescription.Should().Contain("Send the proposal");
            logged.InteractionDescription.Should().Contain("Source: Meeting");
            meeting.ActualOccurredAtUtc.Should().NotBeNull();
        }

        [Fact]
        public async Task Confirm_UnconfirmedMapping_Throws()
        {
            ArrangeStore();
            var meeting = CreatePrep();
            meeting.Status = MeetingStatus.Processed;
            meeting.People.First().MappedPersonId = Guid.NewGuid();
            meeting.People.First().MatchStatus = PersonMatchStatus.Suggested;
            meeting.People.First().SelectedForLogging = true;
            _store.Add(meeting);

            await Assert.ThrowsAsync<InvalidOperationException>(() => Service().ConfirmAsync(new MeetingConfirmRequest { MeetingId = meeting.MeetingId }));
            _interactionsMock.Verify(i => i.LogAsync(It.IsAny<InteractionAddRequest>()), Times.Never);
        }

        [Fact]
        public async Task Delete_Confirmed_Throws()
        {
            ArrangeStore();
            var meeting = CreatePrep();
            meeting.Status = MeetingStatus.Confirmed;
            _store.Add(meeting);
            _memoryRepoMock.Setup(r => r.ListByMeetingAsync(_userA, meeting.MeetingId))
                .ReturnsAsync(new List<RelationshipMemoryEntry>());

            await Assert.ThrowsAsync<InvalidOperationException>(() => Service().DeleteAsync(meeting.MeetingId));
        }

        [Fact]
        public async Task Delete_Draft_PreservesDerivedMemoryWithStamp()
        {
            ArrangeStore();
            var meeting = CreatePrep();
            meeting.Status = MeetingStatus.Draft;
            _store.Add(meeting);
            var derived = new RelationshipMemoryEntry
            {
                MemoryEntryId = Guid.NewGuid(),
                ApplicationUserId = _userA,
                PersonId = Guid.NewGuid(),
                Kind = RelationshipMemoryKind.Fact,
                Title = "Derived fact",
                Provenance = MemoryProvenance.MeetingDerived,
                SourceMeetingId = meeting.MeetingId,
                Status = MemoryEntryStatus.Active
            };
            _memoryRepoMock.Setup(r => r.ListByMeetingAsync(_userA, meeting.MeetingId))
                .ReturnsAsync(new List<RelationshipMemoryEntry> { derived });
            _meetingsMock.Setup(r => r.RemoveAsync(It.IsAny<Meeting>()))
                .Callback<Meeting>(m => _store.Remove(m))
                .Returns(Task.CompletedTask);

            await Service().DeleteAsync(meeting.MeetingId);

            _store.Should().BeEmpty();
            derived.SourceMeetingDeleted.Should().BeTrue();
        }

        [Fact]
        public async Task Process_MapsDetectedPersonToSuggestion()
        {
            ArrangeStore();
            var salmaId = Guid.NewGuid();
            _personsMock.Setup(r => r.GetAllPersons()).ReturnsAsync(new List<Person>
            {
                new() { PersonId = salmaId, ApplicationUserId = _userA, Name = "Salma El-Sayed" }
            }.AsEnumerable());
            _extractorMock.Setup(e => e.ExtractAsync(It.IsAny<MeetingExtractionInput>()))
                .ReturnsAsync(new MeetingExtraction
                {
                    Summary = "Discussed the onboarding revamp.",
                    Topics = new List<string> { "onboarding revamp" },
                    Decisions = new List<string>(),
                    Findings = new List<ExtractedFinding>
                    {
                        new() { Kind = FindingKind.Commitment, Title = "Send crit notes", PersonName = "Salma El-Sayed", SourceExcerpt = "I will send the crit notes" }
                    },
                    DetectedPeople = new List<string> { "Salma El-Sayed" }
                });

            var meeting = CreatePrep();
            meeting.Status = MeetingStatus.Draft;
            meeting.RawNotes = "Salma El-Sayed joined. I will send the crit notes.";
            _store.Add(meeting);

            var result = await Service().ProcessAsync(meeting.MeetingId);

            result.Status.Should().Be(MeetingStatus.Processed);
            var person = result.People.Single(p => p.DetectedName == "Salma El-Sayed");
            person.MatchStatus.Should().Be(PersonMatchStatus.Suggested);
            person.MappedPersonId.Should().Be(salmaId);
            result.Findings.Should().Contain(f => f.Title == "Send crit notes" && f.MappedPersonId == salmaId);
        }

        [Fact]
        public async Task Process_WithExtractor_SucceedsEndToEnd()
        {
            ArrangeStore();
            var salmaId = Guid.NewGuid();
            _personsMock.Setup(r => r.GetAllPersons()).ReturnsAsync(new List<Person>
            {
                new() { PersonId = salmaId, ApplicationUserId = _userA, Name = "Salma El-Sayed" }
            }.AsEnumerable());
            _memoryMock.Setup(m => m.ListForPersonAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<ServiceContracts.DTOs.MemoryDTOs.MemoryEntryResponse>());
            _extractorMock.Setup(e => e.ExtractAsync(It.IsAny<MeetingExtractionInput>()))
                .ReturnsAsync(new MeetingExtraction
                {
                    Summary = "Salma El-Sayed joined.",
                    Topics = new List<string>(),
                    Decisions = new List<string>(),
                    Findings = new List<ExtractedFinding>
                    {
                        new() { Kind = FindingKind.Commitment, Title = "She will send the notes.", PersonName = "Salma El-Sayed", SourceExcerpt = "She will send the notes." }
                    },
                    DetectedPeople = new List<string> { "Salma El-Sayed" }
                });

            var meeting = CreatePrep();
            meeting.Status = MeetingStatus.Draft;
            meeting.RawNotes = "Salma El-Sayed joined. She will send the notes.";
            _store.Add(meeting);

            var result = await Service().ProcessAsync(meeting.MeetingId);

            result.Status.Should().Be(MeetingStatus.Processed);
            result.People.Should().ContainSingle(p => p.DetectedName == "Salma El-Sayed");
        }

        [Fact]
        public async Task Process_WithoutSource_ThrowsAndStaysDraft()
        {
            ArrangeStore();
            var meeting = CreatePrep();
            meeting.Status = MeetingStatus.Draft;
            _store.Add(meeting);

            await Assert.ThrowsAsync<InvalidOperationException>(() => Service().ProcessAsync(meeting.MeetingId));
            meeting.Status.Should().Be(MeetingStatus.Draft);
        }

        [Fact]
        public async Task Process_SkipsFindingDuplicatingActiveMemory()
        {
            ArrangeStore();
            var salmaId = Guid.NewGuid();
            _personsMock.Setup(r => r.GetAllPersons()).ReturnsAsync(new List<Person>
            {
                new() { PersonId = salmaId, ApplicationUserId = _userA, Name = "Salma El-Sayed" }
            }.AsEnumerable());
            _memoryMock.Setup(m => m.ListForPersonAsync(salmaId)).ReturnsAsync(new List<ServiceContracts.DTOs.MemoryDTOs.MemoryEntryResponse>
            {
                new() { MemoryEntryId = Guid.NewGuid(), PersonId = salmaId, Kind = RelationshipMemoryKind.Commitment, Title = "Send crit notes", Status = MemoryEntryStatus.Active, Provenance = MemoryProvenance.User }
            });
            _personsMock.Setup(r => r.GetPersonById(salmaId)).ReturnsAsync(
                new Person { PersonId = salmaId, ApplicationUserId = _userA, Name = "Salma El-Sayed" });
            _extractorMock.Setup(e => e.ExtractAsync(It.IsAny<MeetingExtractionInput>()))
                .ReturnsAsync(new MeetingExtraction
                {
                    Summary = "Same topic again.",
                    Topics = new List<string>(),
                    Decisions = new List<string>(),
                    Findings = new List<ExtractedFinding>
                    {
                        new() { Kind = FindingKind.Commitment, Title = "Send crit notes", PersonName = "Salma El-Sayed" }
                    },
                    DetectedPeople = new List<string> { "Salma El-Sayed" }
                });

            var meeting = CreatePrep();
            meeting.Status = MeetingStatus.Draft;
            meeting.RawNotes = "Salma El-Sayed joined.";
            _store.Add(meeting);

            var result = await Service().ProcessAsync(meeting.MeetingId);

            result.Findings.Should().BeEmpty();
        }

        [Fact]
        public async Task Process_PassesActiveMemoryTitlesToExtractor()
        {
            ArrangeStore();
            var personId = Guid.NewGuid();
            _personsMock.Setup(r => r.GetAllPersons()).ReturnsAsync(new List<Person>
            {
                new() { PersonId = personId, ApplicationUserId = _userA, Name = "Salma" }
            }.AsEnumerable());
            _personsMock.Setup(r => r.GetPersonById(personId)).ReturnsAsync(
                new Person { PersonId = personId, ApplicationUserId = _userA, Name = "Salma" });
            MeetingExtractionInput? captured = null;
            _extractorMock.Setup(e => e.ExtractAsync(It.IsAny<MeetingExtractionInput>()))
                .Callback<MeetingExtractionInput>(i => captured = i)
                .ReturnsAsync(new MeetingExtraction { Summary = "Done." });
            _memoryMock.Setup(m => m.ListForPersonAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<ServiceContracts.DTOs.MemoryDTOs.MemoryEntryResponse>
                {
                    new() { MemoryEntryId = Guid.NewGuid(), Kind = RelationshipMemoryKind.Fact, Title = "User-corrected role", Status = MemoryEntryStatus.Active, Provenance = MemoryProvenance.User }
                });

            var meeting = CreatePrep();
            meeting.Status = MeetingStatus.Draft;
            meeting.RawNotes = "Some notes.";
            _store.Add(meeting);

            await Service().ProcessAsync(meeting.MeetingId);

            captured.Should().NotBeNull();
            captured!.ActiveMemoryTitles.Should().Contain("User-corrected role");
        }

        [Fact]
        public async Task ReviewFinding_Accept_StagesFinding_Confirm_WritesMemoryWithProvenanceChain()
        {
            // Finding-Accept only stages; durable memory
            // is written once at meeting Confirm, so unconfirmed-meeting facts
            // never leak into Copilot/queue.
            ArrangeStore();
            var personId = Guid.NewGuid();
            _personsMock.Setup(r => r.GetPersonById(personId))
                .ReturnsAsync(new Person { PersonId = personId, ApplicationUserId = _userA, Name = "Salma" });

            var meeting = CreatePrep();
            meeting.Status = MeetingStatus.Processed;
            meeting.OccurredAtUtc = DateTime.UtcNow.AddDays(-1);
            meeting.People.First().MappedPersonId = personId;
            meeting.People.First().MatchStatus = PersonMatchStatus.Confirmed;
            meeting.People.First().SelectedForLogging = true;
            var findingId = Guid.NewGuid();
            meeting.Findings.Add(new MeetingFinding
            {
                MeetingFindingId = findingId,
                MeetingId = meeting.MeetingId,
                MappedPersonId = personId,
                Kind = FindingKind.Commitment,
                Title = "Send the proposal",
                Detail = "By Friday",
                Status = FindingStatus.Suggested,
                SourceExcerpt = "I will send it",
                CreatedAtUtc = DateTime.UtcNow
            });
            _store.Add(meeting);

            RelationshipMemoryEntry? saved = null;
            _memoryRepoMock.Setup(r => r.AddAsync(It.IsAny<RelationshipMemoryEntry>()))
                .Callback<RelationshipMemoryEntry>(e => saved = e)
                .Returns(Task.CompletedTask);

            var staged = await Service().ReviewFindingAsync(new FindingReviewRequest
            {
                MeetingFindingId = findingId,
                Status = FindingStatus.Accepted
            });

            staged.Findings.Single(f => f.MeetingFindingId == findingId).Status.Should().Be(FindingStatus.Accepted);
            staged.Findings.Single(f => f.MeetingFindingId == findingId).AcceptedAsEntryId.Should().BeNull();
            _memoryRepoMock.Verify(r => r.AddAsync(It.IsAny<RelationshipMemoryEntry>()), Times.Never);

            _interactionsMock.Setup(i => i.LogAsync(It.IsAny<InteractionAddRequest>()))
                .ReturnsAsync(new ServiceContracts.DTOs.InteractionResponse());

            var confirmed = await Service().ConfirmAsync(new MeetingConfirmRequest { MeetingId = meeting.MeetingId });

            saved.Should().NotBeNull();
            saved!.SourceMeetingId.Should().Be(meeting.MeetingId);
            saved.SourceFindingId.Should().Be(findingId);
            saved.SourceExcerpt.Should().Be("I will send it");
            saved.Provenance.Should().Be(MemoryProvenance.MeetingDerived);
            saved.Kind.Should().Be(RelationshipMemoryKind.Commitment);
            confirmed.Findings.Single(f => f.MeetingFindingId == findingId).AcceptedAsEntryId.Should().Be(saved.MemoryEntryId);
        }

        [Fact]
        public async Task GenerateBrief_ContainsRequiredParticipantSections()
        {
            ArrangeStore();
            var personId = Guid.NewGuid();
            _personsMock.Setup(r => r.GetPersonById(personId))
                .ReturnsAsync(new Person { PersonId = personId, ApplicationUserId = _userA, Name = "Salma", Origin = "Met at work" });
            _scoringMock.Setup(s => s.GetQueueAsync(It.IsAny<int>())).ReturnsAsync(new List<RelationshipHealthResponse>());
            _interactionsMock.Setup(i => i.ListForPersonAsync(It.IsAny<Guid?>()))
                .ReturnsAsync(new List<ServiceContracts.DTOs.InteractionResponse>());
            _memoryMock.Setup(m => m.ListForPersonAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<ServiceContracts.DTOs.MemoryDTOs.MemoryEntryResponse>());
            _eventsMock.Setup(e => e.GetUpcomingAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<ServiceContracts.DTOs.EventDTOs.EventOccurrenceDto>());

            var meeting = CreatePrep();
            meeting.People.First().MappedPersonId = personId;
            _store.Add(meeting);

            var result = await Service().GenerateBriefAsync(meeting.MeetingId);

            result.Brief.Should().NotBeNull();
            var json = System.Text.Json.JsonDocument.Parse(result.Brief!.BriefJson);
            json.RootElement.TryGetProperty("meeting", out _).Should().BeTrue();
            var participants = json.RootElement.GetProperty("participants");
            participants.GetArrayLength().Should().Be(1);
            var first = participants[0];
            foreach (var section in new[] { "displayName", "thingsToRemember", "talkingPoints", "questionsToAsk" })
                first.TryGetProperty(section, out _).Should().BeTrue($"section '{section}' is required");
            Servicess.MeetingBriefValidator.Validate(result.Brief.BriefJson);
        }
    }
}
