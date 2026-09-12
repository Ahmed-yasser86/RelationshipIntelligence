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
                new(plainId, "Plain", new List<string>(), new List<string>(), new List<string>(), new List<string>()),
                new(importantId, "Important", new List<string>(), new List<string>(), new List<string>(), new List<string> { "HighPriority" })
            });
            _statesMock.Setup(r => r.ListForOwnerAsync(_userA)).ReturnsAsync(new List<RelationshipState>
            {
                new() { PersonId = plainId, ApplicationUserId = _userA, UrgencyScore = 70, TieStrength = 0.5 },
                new() { PersonId = importantId, ApplicationUserId = _userA, UrgencyScore = 70, TieStrength = 0.5 }
            });

            var queue = await Service().GetQueueAsync(7);

            queue.Should().HaveCount(2);
            queue[0].PersonId.Should().Be(importantId);
            queue[0].IsImportant.Should().BeTrue();
            queue[0].Band.Should().Be("AtRisk");
        }
    }
}
