using ContactsManager.API.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RelationshipIntelligence.AI;
using ServiceContracts;
using ServiceContracts.DTOs.IngestionDTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace CRUDTests.ControllersTest
{
    public class IngestionControllerTests
    {
        private readonly Mock<IIngestionService> _serviceMock = new();

        private IngestionController Controller() => new(_serviceMock.Object);

        [Fact]
        public async Task GetBatch_Missing_ReturnsNotFound()
        {
            var id = Guid.NewGuid();
            _serviceMock.Setup(s => s.GetAsync(id)).ThrowsAsync(new KeyNotFoundException("nope"));

            var result = await Controller().GetBatch(id);

            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task PostSubmit_BlankText_ReturnsBadRequest()
        {
            _serviceMock.Setup(s => s.SubmitAsync(It.IsAny<IngestionSubmitRequest>()))
                .ThrowsAsync(new ArgumentException("Raw text is required."));

            var result = await Controller().PostSubmit(new IngestionSubmitRequest { RawText = " " });

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task PostProcess_ExtractorDown_ReturnsServiceUnavailable()
        {
            var id = Guid.NewGuid();
            _serviceMock.Setup(s => s.ProcessAsync(id))
                .ThrowsAsync(new CopilotUnavailableException("down"));

            var result = await Controller().PostProcess(id);

            var status = result.Should().BeOfType<ObjectResult>().Subject;
            status.StatusCode.Should().Be(503);
        }

        [Fact]
        public async Task PostProcess_NotConfigured_ReturnsConflict()
        {
            var id = Guid.NewGuid();
            _serviceMock.Setup(s => s.ProcessAsync(id))
                .ThrowsAsync(new CopilotNotConfiguredException());

            var result = await Controller().PostProcess(id);

            result.Should().BeOfType<ConflictObjectResult>();
        }

        [Fact]
        public async Task PostSubmitForMeeting_NoContent_ReturnsBadRequest()
        {
            var id = Guid.NewGuid();
            _serviceMock.Setup(s => s.SubmitForMeetingAsync(id))
                .ThrowsAsync(new InvalidOperationException("Log what happened first."));

            var result = await Controller().PostSubmitForMeeting(id);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task PostApproveFinding_Missing_ReturnsNotFound()
        {
            var id = Guid.NewGuid();
            _serviceMock.Setup(s => s.ApproveFindingAsync(id))
                .ThrowsAsync(new KeyNotFoundException("nope"));

            var result = await Controller().PostApproveFinding(id);

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetPending_ReturnsOk()
        {
            _serviceMock.Setup(s => s.ListPendingAsync())
                .ReturnsAsync(new List<IngestionFindingDto>());

            var result = await Controller().GetPending();

            result.Should().BeOfType<OkObjectResult>();
        }
    }
}
