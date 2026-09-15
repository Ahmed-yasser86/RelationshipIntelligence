using ContactsManger.Core.Domain.Entities;
using ContactsManger.Core.Domain.Entities.EEnums;
using Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RepositryContracts;
using ServiceContracts;
using ServiceContracts.DTOs;
using Servicess;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Xunit;

namespace CRUDTests
{
    public class InteractionServiceTests
    {
        private readonly Guid _userA = Guid.NewGuid();
        private readonly Mock<InteractionRepositoryContract> _interactionsMock = new();
        private readonly Mock<PersonRepositryContract> _personsMock = new();
        private readonly Mock<IRelationshipScoringService> _scoringMock = new();
        private readonly Mock<ICurrentUserService> _userMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

        private InteractionService Service() => new(
            _interactionsMock.Object,
            _personsMock.Object,
            _scoringMock.Object,
            _userMock.Object,
            _unitOfWorkMock.Object,
            Mock.Of<ILogger<InteractionService>>());

        private Person OwnedPerson(Guid? id = null) => new()
        {
            PersonId = id ?? Guid.NewGuid(),
            ApplicationUserId = _userA,
            Name = "LayLa"
        };

        private InteractionAddRequest ValidRequest(Guid personId) => new()
        {
            PersonId = personId,
            TimeOfInteraction = DateTime.UtcNow.AddDays(-2),
            InteractionType = EnInteractionType.Email,
            InteractionTitle = "Q3 hiring plans",
            InteractionDescription = "Discussed headcount"
        };

        [Fact]
        public async Task LogAsync_ValidRequest_SavesAndCommitsOnce()
        {
            _userMock.Setup(u => u.UserId).Returns(_userA);
            var person = OwnedPerson();
            _personsMock.Setup(r => r.GetPersonById(person.PersonId)).ReturnsAsync(person);
            Interaction? saved = null;
            _interactionsMock.Setup(r => r.AddAsync(It.IsAny<Interaction>()))
                .Callback<Interaction>(i => saved = i)
                .ReturnsAsync((Interaction i) => i);

            var result = await Service().LogAsync(ValidRequest(person.PersonId));

            result.Should().NotBeNull();
            result.PersonId.Should().Be(person.PersonId);
            saved!.PersonId.Should().Be(person.PersonId);
            saved.TimeOfInteraction.Kind.Should().Be(DateTimeKind.Utc);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
            _scoringMock.Verify(s => s.RecomputeForPairAsync(person.PersonId), Times.Once);
        }

        [Fact]
        public async Task LogAsync_WithEnabledReminder_ClearsCycleViaRealInteraction()
        {
            // The canonical pipeline is the sole path that completes a
            // reminder cycle. No fake interaction, no silent completion.
            _userMock.Setup(u => u.UserId).Returns(_userA);
            var person = OwnedPerson();
            _personsMock.Setup(r => r.GetPersonById(person.PersonId)).ReturnsAsync(person);
            _interactionsMock.Setup(r => r.AddAsync(It.IsAny<Interaction>()))
                .ReturnsAsync((Interaction i) => i);
            var pref = new RelationshipPreference
            {
                RelationshipPreferenceId = Guid.NewGuid(),
                ApplicationUserId = _userA,
                PersonId = person.PersonId,
                ReminderEnabled = true,
                ReminderIntervalDays = 10,
                SnoozedUntilUtc = DateTime.UtcNow.AddDays(5),
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            var prefsMock = new Mock<RelationshipPreferenceRepositoryContract>();
            prefsMock.Setup(r => r.GetAsync(_userA, person.PersonId)).ReturnsAsync(pref);
            var service = new InteractionService(
                _interactionsMock.Object, _personsMock.Object, _scoringMock.Object,
                _userMock.Object, _unitOfWorkMock.Object,
                Mock.Of<ILogger<InteractionService>>(), prefsMock.Object);

            await service.LogAsync(ValidRequest(person.PersonId));

            pref.LastCompletedAtUtc.Should().NotBeNull();
            pref.SnoozedUntilUtc.Should().BeNull();
        }

        [Fact]
        public async Task LogAsync_ForeignPersonId_ThrowsArgumentException()
        {
            _userMock.Setup(u => u.UserId).Returns(_userA);
            var foreignId = Guid.NewGuid();
            _personsMock.Setup(r => r.GetPersonById(foreignId)).ReturnsAsync((Person?)null);

            await Assert.ThrowsAsync<ArgumentException>(() => Service().LogAsync(ValidRequest(foreignId)));
            _interactionsMock.Verify(r => r.AddAsync(It.IsAny<Interaction>()), Times.Never);
        }

        [Fact]
        public async Task LogAsync_FutureDate_ThrowsValidationException()
        {
            _userMock.Setup(u => u.UserId).Returns(_userA);
            var person = OwnedPerson();
            _personsMock.Setup(r => r.GetPersonById(person.PersonId)).ReturnsAsync(person);

            var request = ValidRequest(person.PersonId);
            request.TimeOfInteraction = DateTime.UtcNow.AddDays(5);

            await Assert.ThrowsAsync<ValidationException>(() => Service().LogAsync(request));
        }

        [Fact]
        public async Task LogAsync_Unauthenticated_ThrowsUnauthorizedAccessException()
        {
            _userMock.Setup(u => u.UserId).Returns((Guid?)null);
            var person = OwnedPerson();
            _personsMock.Setup(r => r.GetPersonById(person.PersonId)).ReturnsAsync(person);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => Service().LogAsync(ValidRequest(person.PersonId)));
        }

        [Fact]
        public async Task ListForPersonAsync_ForeignPersonId_ReturnsEmpty()
        {
            _userMock.Setup(u => u.UserId).Returns(_userA);
            var foreignId = Guid.NewGuid();
            _personsMock.Setup(r => r.GetPersonById(foreignId)).ReturnsAsync((Person?)null);

            var result = await Service().ListForPersonAsync(foreignId);

            result.Should().BeEmpty();
            _interactionsMock.Verify(
                r => r.ListForPersonAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task ImportCsvAsync_ValidCsv_ImportsRowsAndCommitsOnce()
        {
            _userMock.Setup(u => u.UserId).Returns(_userA);
            var person = OwnedPerson();
            _personsMock.Setup(r => r.GetPersonById(person.PersonId)).ReturnsAsync(person);
            var saved = new List<Interaction>();
            _interactionsMock.Setup(r => r.AddAsync(It.IsAny<Interaction>()))
                .Callback<Interaction>(saved.Add)
                .ReturnsAsync((Interaction i) => i);

            string csv = "date,type,title,description\n2026-08-01,Email,Q3 plans,Talked headcount\n2026-08-20,Call,Follow-up,";
            var count = await Service().ImportCsvAsync(person.PersonId, csv);

            count.Should().Be(2);
            saved.Should().HaveCount(2);
            saved.Should().AllSatisfy(i => i.PersonId.Should().Be(person.PersonId));
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task ImportCsvAsync_TooManyRows_ThrowsArgumentException()
        {
            _userMock.Setup(u => u.UserId).Returns(_userA);
            var person = OwnedPerson();
            _personsMock.Setup(r => r.GetPersonById(person.PersonId)).ReturnsAsync(person);

            string csv = "date,type,title\n" + string.Concat(
                System.Linq.Enumerable.Repeat("2026-01-01,Email,x\n", InteractionCsvParser.MaxRows + 1));

            await Assert.ThrowsAsync<ValidationException>(
                () => Service().ImportCsvAsync(person.PersonId, csv));
        }

        [Fact]
        public void CsvParser_InvalidType_ThrowsWithLineNumber()
        {
            Action act = () => InteractionCsvParser.Parse("date,type,title\n2026-01-01,Teleport,Hello");
            act.Should().Throw<ValidationException>().WithMessage("*Line 2*");
        }

        [Fact]
        public void CsvParser_MissingTitle_Throws()
        {
            Action act = () => InteractionCsvParser.Parse("date,type,title\n2026-01-01,Email,");
            act.Should().Throw<ValidationException>();
        }
    }
}
