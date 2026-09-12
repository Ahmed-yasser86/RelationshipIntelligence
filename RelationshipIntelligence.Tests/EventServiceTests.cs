using Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RepositryContracts;
using ServiceContracts;
using ServiceContracts.DTOs.EventDTOs;
using Servicess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CRUDTests
{
    public class EventServiceTests
    {
        private readonly Guid _userA = Guid.NewGuid();
        private readonly Mock<RelationshipEventRepositoryContract> _eventsMock = new();
        private readonly Mock<PersonRepositryContract> _personsMock = new();
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<ICurrentUserService> _userMock = new();

        private EventService Service() => new(
            _eventsMock.Object,
            _personsMock.Object,
            _uowMock.Object,
            _userMock.Object,
            Mock.Of<ILogger<EventService>>());

        [Fact]
        public void NextOccurrence_YearlyBirthday_RollsToNextYear()
        {
            var evt = new RelationshipEvent
            {
                OccursOn = new DateOnly(1990, 3, 10),
                RepeatsYearly = true
            };

            EventService.NextOccurrence(evt, new DateOnly(2026, 9, 12)).Should().Be(new DateOnly(2027, 3, 10));
            EventService.NextOccurrence(evt, new DateOnly(2026, 3, 9)).Should().Be(new DateOnly(2026, 3, 10));
            EventService.NextOccurrence(evt, new DateOnly(2026, 3, 10)).Should().Be(new DateOnly(2026, 3, 10));
        }

        [Fact]
        public void NextOccurrence_Feb29_MapsToFeb28InNonLeapYear()
        {
            var evt = new RelationshipEvent
            {
                OccursOn = new DateOnly(1992, 2, 29),
                RepeatsYearly = true
            };

            EventService.NextOccurrence(evt, new DateOnly(2026, 3, 1)).Should().Be(new DateOnly(2027, 2, 28));
        }

        [Fact]
        public void NextOccurrence_OneOffPast_ReturnsNull()
        {
            var evt = new RelationshipEvent
            {
                OccursOn = new DateOnly(2020, 1, 1),
                RepeatsYearly = false
            };

            EventService.NextOccurrence(evt, new DateOnly(2026, 9, 12)).Should().BeNull();
        }

        [Fact]
        public async Task CreateAsync_ImportanceOutOfRange_Throws()
        {
            var personId = Guid.NewGuid();
            _userMock.Setup(u => u.UserId).Returns(_userA);
            _personsMock.Setup(r => r.GetPersonById(personId))
                .ReturnsAsync(new Person { PersonId = personId, ApplicationUserId = _userA, Name = "P" });

            await Assert.ThrowsAsync<ArgumentException>(() => Service().CreateAsync(new EventCreateRequest
            {
                PersonId = personId,
                Type = RelationshipEventType.Birthday,
                Title = "Birthday",
                OccursOn = new DateOnly(1990, 1, 1),
                Importance = 5
            }));
            _uowMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_OtherOwnersPerson_ThrowsNotFound()
        {
            var personId = Guid.NewGuid();
            _userMock.Setup(u => u.UserId).Returns(_userA);
            _personsMock.Setup(r => r.GetPersonById(personId))
                .ReturnsAsync(new Person { PersonId = personId, ApplicationUserId = Guid.NewGuid(), Name = "P" });

            await Assert.ThrowsAsync<KeyNotFoundException>(() => Service().CreateAsync(new EventCreateRequest
            {
                PersonId = personId,
                Type = RelationshipEventType.Custom,
                Title = "X",
                OccursOn = new DateOnly(2026, 10, 1)
            }));
        }

        [Fact]
        public async Task DeleteAsync_OtherOwnerEvent_ThrowsNotFound()
        {
            var eventId = Guid.NewGuid();
            _userMock.Setup(u => u.UserId).Returns(_userA);
            _eventsMock.Setup(r => r.GetAsync(_userA, eventId)).ReturnsAsync((RelationshipEvent?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => Service().DeleteAsync(eventId));
        }
    }
}
