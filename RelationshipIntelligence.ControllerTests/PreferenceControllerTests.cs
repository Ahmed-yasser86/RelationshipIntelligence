using ContactsManager.API.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ServiceContracts;
using ServiceContracts.DTOs.PreferenceDTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace CRUDTests.ControllersTest
{
    public class PreferenceControllerTests
    {
        private readonly Mock<IRelationshipPreferenceService> _serviceMock = new();

        private PreferenceController Controller() => new(_serviceMock.Object);

        [Fact]
        public async Task GetPreference_NoneSet_ReturnsNotFound()
        {
            var personId = Guid.NewGuid();
            _serviceMock.Setup(s => s.GetAsync(personId))
                .ReturnsAsync((RelationshipPreferenceDto?)null);

            var result = await Controller().GetPreference(personId);

            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task PutPreference_BadInterval_ReturnsBadRequest()
        {
            _serviceMock.Setup(s => s.SaveAsync(It.IsAny<PreferenceSaveRequest>(), It.IsAny<Entities.PreferenceChangeSource>()))
                .ThrowsAsync(new ArgumentException("Interval must be between 1 and 365 days."));

            var result = await Controller().PutPreference(new PreferenceSaveRequest
            {
                PersonId = Guid.NewGuid(),
                DesiredCadenceDays = 999
            });

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task PostSnooze_UnknownPerson_ReturnsNotFound()
        {
            var personId = Guid.NewGuid();
            _serviceMock.Setup(s => s.SnoozeAsync(personId, 3))
                .ThrowsAsync(new KeyNotFoundException("nope"));

            var result = await Controller().PostSnooze(personId, 3);

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetDueReminders_ReturnsOk()
        {
            _serviceMock.Setup(s => s.ListDueAsync())
                .ReturnsAsync(new List<ReminderDueDto>());

            var result = await Controller().GetDueReminders();

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task DeleteReminder_UnknownPerson_ReturnsNotFound()
        {
            var personId = Guid.NewGuid();
            _serviceMock.Setup(s => s.DisableReminderAsync(personId, It.IsAny<Entities.PreferenceChangeSource>()))
                .ThrowsAsync(new KeyNotFoundException("nope"));

            var result = await Controller().DeleteReminder(personId);

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task PostComplete_RequiresRealInteraction_ReturnsConflict()
        {
            // Completion-without-interaction surfaces as 409 with guidance,
            // never a silent completion and never a fake interaction.
            var personId = Guid.NewGuid();
            _serviceMock.Setup(s => s.CompleteAsync(personId))
                .ThrowsAsync(new InvalidOperationException("Log the actual interaction first."));

            var result = await Controller().PostComplete(personId);

            result.Should().BeOfType<ConflictObjectResult>();
        }

        [Fact]
        public async Task DeletePreference_RemovesOverride_ReturnsOk()
        {
            var personId = Guid.NewGuid();
            _serviceMock.Setup(s => s.RemoveAsync(personId, It.IsAny<Entities.PreferenceChangeSource>()))
                .Returns(Task.CompletedTask);

            var result = await Controller().DeletePreference(personId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetReminderState_ReturnsOk()
        {
            var personId = Guid.NewGuid();
            _serviceMock.Setup(s => s.GetReminderStateAsync(personId)).ReturnsAsync("Due");

            var result = await Controller().GetReminderState(personId);

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().Be("Due");
        }

        [Fact]
        public async Task PutGlobalDefaults_BadValue_ReturnsBadRequest()
        {
            _serviceMock.Setup(s => s.SaveGlobalDefaultsAsync(It.IsAny<GlobalDefaultsSaveRequest>(), It.IsAny<Entities.PreferenceChangeSource>()))
                .ThrowsAsync(new ArgumentException("Interval must be between 1 and 365 days."));

            var result = await Controller().PutGlobalDefaults(new GlobalDefaultsSaveRequest { DefaultCadenceDays = 999 });

            result.Should().BeOfType<BadRequestObjectResult>();
        }
    }
}
