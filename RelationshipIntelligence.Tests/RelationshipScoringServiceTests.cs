using ContactsManger.Core.Domain.Entities;
using ContactsManger.Core.Domain.Entities.EEnums;
using Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RepositryContracts;
using ServiceContracts;
using Servicess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CRUDTests
{
    public class RelationshipScoringServiceTests
    {
        private readonly Guid _userA = Guid.NewGuid();
        private readonly Mock<PersonRepositryContract> _personsMock = new();
        private readonly Mock<RelationshipStateRepositoryContract> _statesMock = new();
        private readonly Mock<ICurrentUserService> _userMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

        private RelationshipScoringService Service() => new(
            _personsMock.Object,
            _statesMock.Object,
            _userMock.Object,
            _unitOfWorkMock.Object,
            Mock.Of<ILogger<RelationshipScoringService>>());

        private Person PersonWithEvents(string name, params int[] daysAgo)
        {
            var person = new Person
            {
                PersonId = Guid.NewGuid(),
                ApplicationUserId = _userA,
                Name = name
            };
            foreach (var d in daysAgo)
            {
                person.Interactions.Add(new Interaction
                {
                    InteractionId = Guid.NewGuid(),
                    PersonId = person.PersonId,
                    TimeOfInteraction = DateTime.UtcNow.AddDays(-d),
                    InteractionType = EnInteractionType.Email,
                    InteractionTitle = "Sync"
                });
            }
            return person;
        }

        private void ArrangeUser(params Person[] persons)
        {
            _userMock.Setup(u => u.UserId).Returns(_userA);
            _personsMock.Setup(r => r.GetAllPersons())
                .ReturnsAsync(persons.AsEnumerable());
            _statesMock.Setup(r => r.AddSnapshotAsync(It.IsAny<RelationshipStateSnapshot>()))
                .Returns(Task.CompletedTask);
            _statesMock.Setup(r => r.ListSnapshotsAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .ReturnsAsync(new List<RelationshipStateSnapshot>());
        }

        [Fact]
        public async Task RecomputeForOwnerAsync_UpsertsStatePerPersonAndCommitsOnce()
        {
            var recent = PersonWithEvents("Recent", 1);
            var stale = PersonWithEvents("Stale", 120);
            ArrangeUser(recent, stale);

            var saved = new List<RelationshipState>();
            _statesMock.Setup(r => r.UpsertAsync(It.IsAny<RelationshipState>()))
                .Callback<RelationshipState>(saved.Add)
                .Returns(Task.CompletedTask);

            var count = await Service().RecomputeForOwnerAsync(_userA);

            count.Should().Be(2);
            saved.Should().HaveCount(2);
            saved.Should().OnlyContain(s => s.ApplicationUserId == _userA);
            saved.First(s => s.PersonId == stale.PersonId).UrgencyScore
                .Should().BeGreaterThan(saved.First(s => s.PersonId == recent.PersonId).UrgencyScore);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task RecomputeForOwnerAsync_AppendsOneSnapshotPerDay()
        {
            var person = PersonWithEvents("Snap", 3);
            ArrangeUser(person);

            var snapshots = new List<RelationshipState>();
            _statesMock.Setup(r => r.UpsertAsync(It.IsAny<RelationshipState>()))
                .Callback<RelationshipState>(snapshots.Add)
                .Returns(Task.CompletedTask);

            int addedSnapshots = 0;
            _statesMock.Setup(r => r.AddSnapshotAsync(It.IsAny<RelationshipStateSnapshot>()))
                .Callback(() => addedSnapshots++)
                .Returns(Task.CompletedTask);
            _statesMock.Setup(r => r.ListSnapshotsAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .ReturnsAsync(new List<RelationshipStateSnapshot>());

            await Service().RecomputeForOwnerAsync(_userA);

            addedSnapshots.Should().Be(1);
        }

        [Fact]
        public async Task GetHistoryAsync_ReturnsOrderedPoints()
        {
            _userMock.Setup(u => u.UserId).Returns(_userA);
            var personId = Guid.NewGuid();
            _personsMock.Setup(r => r.GetPersonById(personId)).ReturnsAsync(
                new Person { PersonId = personId, ApplicationUserId = _userA, Name = "H" });
            _statesMock.Setup(r => r.ListSnapshotsAsync(_userA, personId)).ReturnsAsync(
                new List<RelationshipStateSnapshot>
                {
                    new() { PersonId = personId, ApplicationUserId = _userA, TakenAtUtc = DateTime.UtcNow.AddDays(-2), TieStrength = 2, UrgencyScore = 10, Band = "Healthy" },
                    new() { PersonId = personId, ApplicationUserId = _userA, TakenAtUtc = DateTime.UtcNow.AddDays(-1), TieStrength = 1, UrgencyScore = 55, Band = "Drifting" }
                });

            var history = await Service().GetHistoryAsync(personId);

            history.PersonId.Should().Be(personId);
            history.Points.Should().HaveCount(2);
            history.Points[0].TakenAtUtc.Should().BeBefore(history.Points[1].TakenAtUtc);
        }

        [Fact]
        public async Task GetHistoryAsync_ForeignPerson_ReturnsEmpty()
        {
            _userMock.Setup(u => u.UserId).Returns(_userA);
            var foreignId = Guid.NewGuid();
            _personsMock.Setup(r => r.GetPersonById(foreignId)).ReturnsAsync((Person?)null);

            var history = await Service().GetHistoryAsync(foreignId);

            history.Points.Should().BeEmpty();
        }

        [Fact]
        public async Task RecomputePairsAsync_EmptySet_DoesNothing()
        {
            _userMock.Setup(u => u.UserId).Returns(_userA);

            (await Service().RecomputePairsAsync(new List<Guid>())).Should().Be(0);
            _statesMock.Verify(r => r.UpsertAsync(It.IsAny<RelationshipState>()), Times.Never);
        }

        [Fact]
        public async Task RecomputePairsAsync_ScoresOnlyRequestedPairs()
        {
            _userMock.Setup(u => u.UserId).Returns(_userA);
            var wanted = PersonWithEvents("Wanted", 10);
            var other = PersonWithEvents("Other", 10);
            _personsMock.Setup(r => r.ListByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
                .ReturnsAsync((IEnumerable<Guid> ids) => new[] { wanted, other }
                    .Where(p => ids.Contains(p.PersonId)).ToList());
            _statesMock.Setup(r => r.ListForOwnerAsync(_userA))
                .ReturnsAsync(new List<RelationshipState>());

            var saved = new List<RelationshipState>();
            _statesMock.Setup(r => r.UpsertAsync(It.IsAny<RelationshipState>()))
                .Callback<RelationshipState>(saved.Add)
                .Returns(Task.CompletedTask);
            _statesMock.Setup(r => r.AddSnapshotAsync(It.IsAny<RelationshipStateSnapshot>()))
                .Returns(Task.CompletedTask);
            _statesMock.Setup(r => r.ListSnapshotsAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .ReturnsAsync(new List<RelationshipStateSnapshot>());

            var count = await Service().RecomputePairsAsync(new[] { wanted.PersonId });

            count.Should().Be(1);
            saved.Should().ContainSingle(s => s.PersonId == wanted.PersonId);
        }

        [Fact]
        public async Task RecomputeForOwnerAsync_EmptyOwner_ReturnsZero()
        {
            (await Service().RecomputeForOwnerAsync(Guid.Empty)).Should().Be(0);
            _statesMock.Verify(r => r.UpsertAsync(It.IsAny<RelationshipState>()), Times.Never);
        }

        [Fact]
        public async Task RecomputeForOwnerAsync_NoScoreTag_SkipsPerson()
        {
            var skipped = PersonWithEvents("Skipped", 100);
            skipped.UserDefinedTags.Add(new UserDefinedTags { TagId = Guid.NewGuid(), TagName = "noscore" });
            var kept = PersonWithEvents("Kept", 100);
            ArrangeUser(skipped, kept);

            var saved = new List<RelationshipState>();
            _statesMock.Setup(r => r.UpsertAsync(It.IsAny<RelationshipState>()))
                .Callback<RelationshipState>(saved.Add)
                .Returns(Task.CompletedTask);

            await Service().RecomputeForOwnerAsync(_userA);

            saved.Should().ContainSingle(s => s.PersonId == kept.PersonId);
        }

        [Fact]
        public async Task RecomputeForCurrentUserAsync_Unauthenticated_Throws()
        {
            _userMock.Setup(u => u.UserId).Returns((Guid?)null);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => Service().RecomputeForCurrentUserAsync());
        }

        [Fact]
        public async Task RecomputeForPairAsync_ForeignPerson_DoesNothing()
        {
            _userMock.Setup(u => u.UserId).Returns(_userA);
            var foreignId = Guid.NewGuid();
            _personsMock.Setup(r => r.GetPersonById(foreignId)).ReturnsAsync((Person?)null);

            await Service().RecomputeForPairAsync(foreignId);

            _statesMock.Verify(r => r.UpsertAsync(It.IsAny<RelationshipState>()), Times.Never);
        }

        [Fact]
        public async Task GetQueueAsync_OrdersByUrgencyWithImportantTieBreak()
        {
            _userMock.Setup(u => u.UserId).Returns(_userA);
            var plainId = Guid.NewGuid();
            var importantId = Guid.NewGuid();
            _personsMock.Setup(r => r.ListAffinitiesAsync()).ReturnsAsync(new List<PersonAffinity>
            {
                new(plainId, "Plain", new List<string>(), new List<string>(), new List<string>(), new List<string>(), null),
                new(importantId, "Important", new List<string>(), new List<string>(), new List<string>(), new List<string> { "HighPriority" }, null)
            });
            _statesMock.Setup(r => r.ListForOwnerAsync(_userA)).ReturnsAsync(new List<RelationshipState>
            {
                new() { PersonId = plainId, ApplicationUserId = _userA, UrgencyScore = 70, TieStrength = 0.5, EvidenceStatus = EvidenceStatus.Established },
                new() { PersonId = importantId, ApplicationUserId = _userA, UrgencyScore = 70, TieStrength = 0.5, EvidenceStatus = EvidenceStatus.Established }
            });

            var queue = await Service().GetQueueAsync(7);

            queue.Should().HaveCount(2);
            queue[0].PersonId.Should().Be(importantId);
            queue[0].IsImportant.Should().BeTrue();
            queue[0].Band.Should().Be("AtRisk");
        }

        [Fact]
        public async Task GetQueueAsync_EventNearAndDrifting_SetsEventSignalWithoutChangingScore()
        {
            _userMock.Setup(u => u.UserId).Returns(_userA);
            var driftedId = Guid.NewGuid();
            var calmId = Guid.NewGuid();
            _personsMock.Setup(r => r.ListAffinitiesAsync()).ReturnsAsync(new List<PersonAffinity>
            {
                new(driftedId, "Drifted", new List<string>(), new List<string>(), new List<string>(), new List<string>(), null),
                new(calmId, "Calm", new List<string>(), new List<string>(), new List<string>(), new List<string>(), null)
            });
            _statesMock.Setup(r => r.ListForOwnerAsync(_userA)).ReturnsAsync(new List<RelationshipState>
            {
                new() { PersonId = driftedId, ApplicationUserId = _userA, UrgencyScore = 80, TieStrength = 0.4, EvidenceStatus = EvidenceStatus.Established, LastContactAtUtc = DateTime.UtcNow.AddDays(-81), CadenceReferenceDays = 30 },
                new() { PersonId = calmId, ApplicationUserId = _userA, UrgencyScore = 18, TieStrength = 2.5, EvidenceStatus = EvidenceStatus.Established, LastContactAtUtc = DateTime.UtcNow.AddDays(-1), CadenceReferenceDays = 7 }
            });
            var eventsMock = new Mock<IEventService>();
            eventsMock.Setup(e => e.GetUpcomingAsync(21)).ReturnsAsync(new List<ServiceContracts.DTOs.EventDTOs.EventOccurrenceDto>
            {
                new() { EventId = Guid.NewGuid(), PersonId = driftedId, Title = "Birthday", OccurrenceDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(4)), InDays = 4, Importance = 3 },
                new() { EventId = Guid.NewGuid(), PersonId = calmId, Title = "Anniversary", OccurrenceDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)), InDays = 3, Importance = 2 }
            });
            var service = new RelationshipScoringService(
                _personsMock.Object,
                _statesMock.Object,
                _userMock.Object,
                _unitOfWorkMock.Object,
                Mock.Of<ILogger<RelationshipScoringService>>(),
                eventsMock.Object);

            var queue = await service.GetQueueAsync(7);

            var drifted = queue.Single(q => q.PersonId == driftedId);
            drifted.UrgencyScore.Should().Be(80);
            drifted.UpcomingEvents.Should().ContainSingle(o => o.Title == "Birthday");
            drifted.HasEventSignal.Should().BeTrue();

            var calm = queue.Single(q => q.PersonId == calmId);
            calm.UrgencyScore.Should().Be(18);
            calm.UpcomingEvents.Should().ContainSingle(o => o.Title == "Anniversary");
            calm.HasEventSignal.Should().BeFalse();
        }

        [Fact]
        public async Task RecomputeForOwnerAsync_NoHistoryPerson_IsNeutralAndUnexposed()
        {
            var ghost = new Person
            {
                PersonId = Guid.NewGuid(),
                ApplicationUserId = _userA,
                Name = "Ghost"
            };
            ArrangeUser(ghost);

            RelationshipState? saved = null;
            _statesMock.Setup(r => r.UpsertAsync(It.IsAny<RelationshipState>()))
                .Callback<RelationshipState>(s => saved = s)
                .Returns(Task.CompletedTask);
            int snapshots = 0;
            _statesMock.Setup(r => r.AddSnapshotAsync(It.IsAny<RelationshipStateSnapshot>()))
                .Callback(() => snapshots++)
                .Returns(Task.CompletedTask);

            await Service().RecomputeForOwnerAsync(_userA);

            saved.Should().NotBeNull();
            saved!.EvidenceStatus.Should().Be(EvidenceStatus.NoHistory);
            saved.UrgencyScore.Should().Be(0);
            saved.CadenceReferenceDays.Should().BeNull();
            saved.SilenceQuantile.Should().BeNull();
            saved.InteractionCount.Should().Be(0);
            snapshots.Should().Be(0);

            _statesMock.Setup(r => r.ListForOwnerAsync(_userA))
                .ReturnsAsync(new List<RelationshipState> { saved });
            _personsMock.Setup(r => r.ListAffinitiesAsync()).ReturnsAsync(new List<PersonAffinity>
            {
                new(ghost.PersonId, ghost.Name, new List<string>(), new List<string>(), new List<string>(), new List<string>(), null)
            });

            (await Service().GetQueueAsync(7)).Should().BeEmpty();
        }

        [Fact]
        public async Task RecomputeForOwnerAsync_SingleEvent_IsInsufficientWithNullCadence()
        {
            var fresh = PersonWithEvents("Fresh", 2);
            ArrangeUser(fresh);

            RelationshipState? saved = null;
            _statesMock.Setup(r => r.UpsertAsync(It.IsAny<RelationshipState>()))
                .Callback<RelationshipState>(s => saved = s)
                .Returns(Task.CompletedTask);

            await Service().RecomputeForOwnerAsync(_userA);

            saved.Should().NotBeNull();
            saved!.EvidenceStatus.Should().Be(EvidenceStatus.Insufficient);
            saved.InteractionCount.Should().Be(1);
            saved.CadenceReferenceDays.Should().BeNull();
            saved.SilenceQuantile.Should().BeNull();
            saved.LastContactAtUtc.Should().NotBeNull();
        }

        [Fact]
        public async Task RecomputeForOwnerAsync_TwoEvents_IsInsufficientWithPriorCadence()
        {
            var pair = PersonWithEvents("Pair", 30, 10);
            ArrangeUser(pair);

            RelationshipState? saved = null;
            _statesMock.Setup(r => r.UpsertAsync(It.IsAny<RelationshipState>()))
                .Callback<RelationshipState>(s => saved = s)
                .Returns(Task.CompletedTask);

            await Service().RecomputeForOwnerAsync(_userA);

            saved!.EvidenceStatus.Should().Be(EvidenceStatus.Insufficient);
            saved.CadenceReferenceDays.Should().Be(PersonaPriors.DefaultDays);
            saved.SilenceQuantile.Should().NotBeNull();
        }

        [Fact]
        public async Task RecomputeForOwnerAsync_FourEvents_IsEstablished()
        {
            var steady = PersonWithEvents("Steady", 40, 30, 20, 10);
            ArrangeUser(steady);

            RelationshipState? saved = null;
            _statesMock.Setup(r => r.UpsertAsync(It.IsAny<RelationshipState>()))
                .Callback<RelationshipState>(s => saved = s)
                .Returns(Task.CompletedTask);

            await Service().RecomputeForOwnerAsync(_userA);

            saved!.EvidenceStatus.Should().Be(EvidenceStatus.Established);
            saved.InteractionCount.Should().Be(4);
        }

        [Fact]
        public async Task Urgency_WeakTieAgainstStrongTie_IsHighButFlaggedInsufficient()
        {
            var strong = PersonWithEvents("Strong", 1, 2, 3, 4, 5);
            var weak = PersonWithEvents("Weak", 200);
            ArrangeUser(strong, weak);

            var saved = new List<RelationshipState>();
            _statesMock.Setup(r => r.UpsertAsync(It.IsAny<RelationshipState>()))
                .Callback<RelationshipState>(saved.Add)
                .Returns(Task.CompletedTask);

            await Service().RecomputeForOwnerAsync(_userA);

            var weakState = saved.First(s => s.PersonId == weak.PersonId);
            weakState.UrgencyScore.Should().BeGreaterThan(90);
            weakState.EvidenceStatus.Should().Be(EvidenceStatus.Insufficient);
            saved.First(s => s.PersonId == strong.PersonId).EvidenceStatus
                .Should().Be(EvidenceStatus.Established);
        }

        [Fact]
        public async Task RecomputeForOwnerAsync_EmptyDataset_ReturnsZeroAndQueueEmpty()
        {
            ArrangeUser();

            (await Service().RecomputeForOwnerAsync(_userA)).Should().Be(0);
            _statesMock.Setup(r => r.ListForOwnerAsync(_userA))
                .ReturnsAsync(new List<RelationshipState>());
            _personsMock.Setup(r => r.ListAffinitiesAsync())
                .ReturnsAsync(new List<PersonAffinity>());

            (await Service().GetQueueAsync(7)).Should().BeEmpty();
        }
    }
}
