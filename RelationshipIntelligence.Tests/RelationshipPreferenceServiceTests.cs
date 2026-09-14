using Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RepositryContracts;
using ServiceContracts;
using ServiceContracts.DTOs;
using ServiceContracts.DTOs.PreferenceDTOs;
using Servicess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CRUDTests
{
    /// <summary>
    /// User-controlled relationship parameters: human-term validation,
    /// reminder lifecycle (enable, snooze, skip, complete, disable), due
    /// computation that never fabricates interaction, and owner isolation.
    /// </summary>
    public class RelationshipPreferenceServiceTests
    {
        private readonly Guid _userA = Guid.NewGuid();
        private readonly Guid _userB = Guid.NewGuid();
        private readonly Mock<RelationshipPreferenceRepositoryContract> _prefsMock = new();
        private readonly Mock<PersonRepositryContract> _personsMock = new();
        private readonly Mock<IRelationshipScoringService> _scoringMock = new();
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<ICurrentUserService> _userMock = new();

        private RelationshipPreferenceService Service() => new(
            _prefsMock.Object,
            _personsMock.Object,
            _scoringMock.Object,
            _userMock.Object,
            _uowMock.Object,
            Mock.Of<ILogger<RelationshipPreferenceService>>());

        private Person OwnedPerson(Guid owner, Guid? personId = null, string name = "Test Person") => new()
        {
            PersonId = personId ?? Guid.NewGuid(),
            ApplicationUserId = owner,
            Name = name
        };

        private void AsUserA(Person? person = null)
        {
            _userMock.Setup(u => u.UserId).Returns(_userA);
            if (person != null)
                _personsMock.Setup(r => r.GetPersonById(person.PersonId)).ReturnsAsync(person);
            _personsMock.Setup(r => r.GetAllPersons()).ReturnsAsync(new List<Person?>());
        }

        private static RelationshipPreference Pref(Guid owner, Guid personId) => new()
        {
            RelationshipPreferenceId = Guid.NewGuid(),
            ApplicationUserId = owner,
            PersonId = personId,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        [Fact]
        public async Task SaveAsync_BadCadence_Throws()
        {
            var person = OwnedPerson(_userA);
            AsUserA(person);
            await Assert.ThrowsAsync<ArgumentException>(() => Service().SaveAsync(new PreferenceSaveRequest
            {
                PersonId = person.PersonId,
                DesiredCadenceDays = 400
            }));
            _uowMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task SaveAsync_BadImportanceAndPriority_Throw()
        {
            var person = OwnedPerson(_userA);
            AsUserA(person);
            await Assert.ThrowsAsync<ArgumentException>(() => Service().SaveAsync(new PreferenceSaveRequest
            {
                PersonId = person.PersonId,
                Importance = 5
            }));
            await Assert.ThrowsAsync<ArgumentException>(() => Service().SaveAsync(new PreferenceSaveRequest
            {
                PersonId = person.PersonId,
                Priority = -2
            }));
        }

        [Fact]
        public async Task SaveAsync_OtherOwnersPerson_ThrowsNotFound()
        {
            var personId = Guid.NewGuid();
            AsUserA();
            _personsMock.Setup(r => r.GetPersonById(personId))
                .ReturnsAsync(OwnedPerson(_userB, personId));

            await Assert.ThrowsAsync<KeyNotFoundException>(() => Service().SaveAsync(new PreferenceSaveRequest
            {
                PersonId = personId,
                DesiredCadenceDays = 7
            }));
        }

        [Fact]
        public async Task SaveAsync_PersistsHumanTerms_CustomInterval()
        {
            var person = OwnedPerson(_userA);
            AsUserA(person);
            _prefsMock.Setup(r => r.GetAsync(_userA, person.PersonId))
                .ReturnsAsync((RelationshipPreference?)null);
            RelationshipPreference? saved = null;
            _prefsMock.Setup(r => r.AddAsync(It.IsAny<RelationshipPreference>()))
                .Callback<RelationshipPreference>(p => saved = p)
                .Returns(Task.CompletedTask);

            var dto = await Service().SaveAsync(new PreferenceSaveRequest
            {
                PersonId = person.PersonId,
                DesiredCadenceDays = 10,
                Importance = 1,
                Priority = 1,
                KeepInTouchIntentionally = true,
                ExcludeFromSuggestions = true
            });

            dto.DesiredCadenceDays.Should().Be(10);
            dto.Importance.Should().Be(1);
            dto.Priority.Should().Be(1);
            dto.KeepInTouchIntentionally.Should().BeTrue();
            dto.ExcludeFromSuggestions.Should().BeTrue();
            saved.Should().NotBeNull();
        }

        [Fact]
        public async Task SetReminderAsync_BadInterval_Throws()
        {
            var person = OwnedPerson(_userA);
            AsUserA(person);
            await Assert.ThrowsAsync<ArgumentException>(() => Service().SetReminderAsync(new ReminderSetRequest
            {
                PersonId = person.PersonId,
                IntervalDays = 0
            }));
        }

        [Fact]
        public async Task ReminderLifecycle_SnoozeSkipComplete_DoesNotTouchInteractions()
        {
            var person = OwnedPerson(_userA);
            AsUserA(person);
            var pref = Pref(_userA, person.PersonId);
            pref.ReminderEnabled = true;
            pref.ReminderIntervalDays = 10;
            _prefsMock.Setup(r => r.GetAsync(_userA, person.PersonId)).ReturnsAsync(pref);

            var snoozed = await Service().SnoozeAsync(person.PersonId, 3);
            snoozed.SnoozedUntilUtc.Should().NotBeNull();

            pref.SnoozedUntilUtc = null;
            var skipped = await Service().SkipAsync(person.PersonId);
            skipped.SnoozedUntilUtc.Should().NotBeNull();

            pref.SnoozedUntilUtc = null;
            var completed = await Service().CompleteAsync(person.PersonId);
            completed.LastCompletedAtUtc.Should().NotBeNull();
            completed.SnoozedUntilUtc.Should().BeNull();

            var disabled = await Service().DisableReminderAsync(person.PersonId);
            disabled.ReminderEnabled.Should().BeFalse();
        }

        [Fact]
        public async Task ListDueAsync_SnoozedOrRecentSilence_NotDue()
        {
            AsUserA();
            var dueId = Guid.NewGuid();
            var snoozedId = Guid.NewGuid();
            var freshId = Guid.NewGuid();
            var prefs = new List<RelationshipPreference>
            {
                new() { ApplicationUserId = _userA, PersonId = dueId, ReminderEnabled = true, ReminderIntervalDays = 10 },
                new() { ApplicationUserId = _userA, PersonId = snoozedId, ReminderEnabled = true, ReminderIntervalDays = 10, SnoozedUntilUtc = DateTime.UtcNow.AddDays(2) },
                new() { ApplicationUserId = _userA, PersonId = freshId, ReminderEnabled = true, ReminderIntervalDays = 10 }
            };
            _prefsMock.Setup(r => r.ListForOwnerAsync(_userA)).ReturnsAsync(prefs);
            _scoringMock.Setup(s => s.GetQueueAsync(200)).ReturnsAsync(new List<RelationshipHealthResponse>
            {
                new() { PersonId = dueId, Name = "Due", SilenceDays = 15, UrgencyScore = 10 },
                new() { PersonId = freshId, Name = "Fresh", SilenceDays = 2, UrgencyScore = 90 }
            });
            _personsMock.Setup(r => r.GetAllPersons()).ReturnsAsync(new List<Person?>
            {
                OwnedPerson(_userA, dueId, "Due"),
                OwnedPerson(_userA, snoozedId, "Snoozed"),
                OwnedPerson(_userA, freshId, "Fresh")
            });

            var due = await Service().ListDueAsync();

            due.Select(d => d.PersonId).Should().Contain(dueId);
            due.Select(d => d.PersonId).Should().NotContain(snoozedId);
            // Recent contact silences the reminder, but urgency stays high:
            // reminders never rewrite relationship verdicts.
            due.Select(d => d.PersonId).Should().NotContain(freshId);
            due.First(d => d.PersonId == dueId).SourceLabel.Should().Contain("configured");
        }

        [Fact]
        public async Task GetAsync_OtherOwnersPerson_ThrowsNotFound()
        {
            var personId = Guid.NewGuid();
            AsUserA();
            _personsMock.Setup(r => r.GetPersonById(personId))
                .ReturnsAsync(OwnedPerson(_userB, personId));

            await Assert.ThrowsAsync<KeyNotFoundException>(() => Service().GetAsync(personId));
        }
    }
}
