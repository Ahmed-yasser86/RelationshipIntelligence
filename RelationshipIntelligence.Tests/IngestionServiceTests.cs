using Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RepositryContracts;
using ServiceContracts;
using ServiceContracts.DTOs.IngestionDTOs;
using Servicess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CRUDTests
{
    /// <summary>
    /// Unified ingestion pipeline: idempotency, no-op results, conflict and
    /// ambiguity surfacing, approval gating, meeting-evidence separation, and
    /// owner isolation. Extraction never writes durable state in any test.
    /// </summary>
    public class IngestionServiceTests
    {
        private readonly Guid _userA = Guid.NewGuid();
        private readonly Guid _userB = Guid.NewGuid();
        private readonly Mock<IngestionRepositoryContract> _batchesMock = new();
        private readonly Mock<MeetingRepositoryContract> _meetingsMock = new();
        private readonly Mock<PersonRepositryContract> _personsMock = new();
        private readonly Mock<IPersonSearcherService> _searcherMock = new();
        private readonly Mock<IPersonQuickAdderService> _quickAddMock = new();
        private readonly Mock<IPersonUpdaterService> _updaterMock = new();
        private readonly Mock<RelationshipMemoryRepositoryContract> _memoryRepoMock = new();
        private readonly Mock<IRelationshipMemoryService> _memoryMock = new();
        private readonly Mock<IEventService> _eventsMock = new();
        private readonly Mock<IIngestionExtractor> _extractorMock = new();
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<ICurrentUserService> _userMock = new();

        private IngestionService Service() => new(
            _batchesMock.Object,
            _meetingsMock.Object,
            _personsMock.Object,
            _searcherMock.Object,
            _quickAddMock.Object,
            _updaterMock.Object,
            _memoryRepoMock.Object,
            _memoryMock.Object,
            _eventsMock.Object,
            _extractorMock.Object,
            _userMock.Object,
            _uowMock.Object,
            Mock.Of<ILogger<IngestionService>>());

        private void AsUserA()
        {
            _userMock.Setup(u => u.UserId).Returns(_userA);
            _personsMock.Setup(r => r.GetAllPersons())
                .ReturnsAsync(new List<Person?>());
            _memoryMock.Setup(m => m.ListForPersonAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<ServiceContracts.DTOs.MemoryDTOs.MemoryEntryResponse>());
        }

        private static IngestionBatch Batch(Guid owner, Guid? id = null) => new()
        {
            IngestionBatchId = id ?? Guid.NewGuid(),
            ApplicationUserId = owner,
            SourceType = IngestionSourceType.PersonText,
            SourceTextHash = "hash",
            RawText = "raw",
            Status = IngestionStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        [Fact]
        public async Task SubmitAsync_BlankText_Throws()
        {
            AsUserA();
            await Assert.ThrowsAsync<ArgumentException>(() => Service().SubmitAsync(new IngestionSubmitRequest
            {
                SourceType = IngestionSourceType.PersonText,
                RawText = "   "
            }));
            _uowMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task SubmitAsync_TooLong_Throws()
        {
            AsUserA();
            await Assert.ThrowsAsync<ArgumentException>(() => Service().SubmitAsync(new IngestionSubmitRequest
            {
                SourceType = IngestionSourceType.PersonText,
                RawText = new string('x', IngestionService.MaxRawChars + 1)
            }));
        }

        [Fact]
        public async Task SubmitAsync_SameTextTwice_ReturnsExistingBatch()
        {
            AsUserA();
            var existing = Batch(_userA);
            _batchesMock.Setup(r => r.FindByHashAsync(_userA, IngestionSourceType.PersonText, null, It.IsAny<string>()))
                .ReturnsAsync(existing);
            _batchesMock.Setup(r => r.GetAsync(_userA, existing.IngestionBatchId))
                .ReturnsAsync(existing);

            var first = await Service().SubmitAsync(new IngestionSubmitRequest
            {
                SourceType = IngestionSourceType.PersonText,
                RawText = "Mohamed works at Microsoft."
            });
            var second = await Service().SubmitAsync(new IngestionSubmitRequest
            {
                SourceType = IngestionSourceType.PersonText,
                RawText = "  mohamed   works at microsoft. "
            });

            first.IngestionBatchId.Should().Be(existing.IngestionBatchId);
            second.IngestionBatchId.Should().Be(existing.IngestionBatchId);
            _batchesMock.Verify(r => r.AddBatchAsync(It.IsAny<IngestionBatch>()), Times.Never);
        }

        [Fact]
        public async Task ProcessAsync_NoReliableInformation_ExplicitNoOp()
        {
            AsUserA();
            var batch = Batch(_userA);
            _batchesMock.Setup(r => r.GetAsync(_userA, batch.IngestionBatchId)).ReturnsAsync(batch);
            _extractorMock.Setup(e => e.ExtractAsync(It.IsAny<IngestionExtractionInput>()))
                .ReturnsAsync(new IngestionExtraction { NoOp = true, NoOpReason = "Greeting only." });

            var result = await Service().ProcessAsync(batch.IngestionBatchId);

            result.IsNoOp.Should().BeTrue();
            result.NoOpReason.Should().Be("Greeting only.");
            _batchesMock.Verify(r => r.AddFindingAsync(It.IsAny<IngestionFinding>()), Times.Never);
        }

        [Fact]
        public async Task ProcessAsync_UnsupportedField_MarkedUnresolvedNeverInvented()
        {
            AsUserA();
            var batch = Batch(_userA);
            _batchesMock.Setup(r => r.GetAsync(_userA, batch.IngestionBatchId)).ReturnsAsync(batch);
            _extractorMock.Setup(e => e.ExtractAsync(It.IsAny<IngestionExtractionInput>()))
                .ReturnsAsync(new IngestionExtraction
                {
                    Entities = new List<ExtractedIngestionEntity>
                    {
                        new() { Name = "Sara", IsNew = true, Confidence = "High" }
                    },
                    Findings = new List<ExtractedIngestionFinding>
                    {
                        new() { Subject = "Sara", TargetField = "favorite-color", Title = "Blue", Confidence = "High" }
                    }
                });
            var added = new List<IngestionFinding>();
            _batchesMock.Setup(r => r.AddFindingAsync(It.IsAny<IngestionFinding>()))
                .Callback<IngestionFinding>(f => added.Add(f))
                .Returns(Task.CompletedTask);

            await Service().ProcessAsync(batch.IngestionBatchId);

            added.Should().ContainSingle();
            added[0].Status.Should().Be(IngestionFindingStatus.Unresolved);
            added[0].UncertaintyReason.Should().Contain("Unsupported field");
        }

        [Fact]
        public async Task ProcessAsync_AmbiguousName_StaysUnresolvedWithCandidates()
        {
            AsUserA();
            var p1 = new Person { PersonId = Guid.NewGuid(), ApplicationUserId = _userA, Name = "Ahmed Hassan" };
            var p2 = new Person { PersonId = Guid.NewGuid(), ApplicationUserId = _userA, Name = "Ahmed Ali" };
            _personsMock.Setup(r => r.GetAllPersons())
                .ReturnsAsync(new List<Person?> { p1, p2 });
            var batch = Batch(_userA);
            _batchesMock.Setup(r => r.GetAsync(_userA, batch.IngestionBatchId)).ReturnsAsync(batch);
            _extractorMock.Setup(e => e.ExtractAsync(It.IsAny<IngestionExtractionInput>()))
                .ReturnsAsync(new IngestionExtraction
                {
                    Entities = new List<ExtractedIngestionEntity>
                    {
                        new() { Name = "Ahmed", Confidence = "Medium" }
                    },
                    Findings = new List<ExtractedIngestionFinding>
                    {
                        new() { Subject = "Ahmed", TargetField = "location", Title = "Cairo", Confidence = "Medium" }
                    }
                });
            var added = new List<IngestionFinding>();
            _batchesMock.Setup(r => r.AddFindingAsync(It.IsAny<IngestionFinding>()))
                .Callback<IngestionFinding>(f => added.Add(f))
                .Returns(Task.CompletedTask);

            var result = await Service().ProcessAsync(batch.IngestionBatchId);

            added.Should().ContainSingle();
            added[0].Status.Should().Be(IngestionFindingStatus.Unresolved);
            result.Findings.Should().BeEmpty();
        }

        [Fact]
        public async Task ReviewAsync_OtherOwnersFinding_NotFound()
        {
            AsUserA();
            _batchesMock.Setup(r => r.GetFindingAsync(_userA, It.IsAny<Guid>()))
                .ReturnsAsync((IngestionFinding?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => Service().ReviewAsync(
                new IngestionFindingReviewRequest
                {
                    FindingId = Guid.NewGuid(),
                    Status = IngestionFindingStatus.Approved
                }));
        }

        [Fact]
        public async Task ApproveFindingAsync_UnresolvedFinding_SkippedNotApplied()
        {
            AsUserA();
            var finding = new IngestionFinding
            {
                IngestionFindingId = Guid.NewGuid(),
                IngestionBatchId = Guid.NewGuid(),
                ApplicationUserId = _userA,
                Title = "Cairo",
                Status = IngestionFindingStatus.Unresolved,
                Confidence = FindingConfidence.Unresolved
            };
            _batchesMock.Setup(r => r.GetFindingAsync(_userA, finding.IngestionFindingId))
                .ReturnsAsync(finding);
            var batch = Batch(_userA, finding.IngestionBatchId);
            batch.Findings.Add(finding);
            _batchesMock.Setup(r => r.GetAsync(_userA, finding.IngestionBatchId)).ReturnsAsync(batch);

            var result = await Service().ApproveFindingAsync(finding.IngestionFindingId);

            result.AppliedCount.Should().Be(0);
            result.SkippedCount.Should().Be(1);
            _memoryRepoMock.Verify(r => r.AddAsync(It.IsAny<RelationshipMemoryEntry>()), Times.Never);
        }

        [Fact]
        public async Task SubmitForMeetingAsync_NoActualContent_Throws()
        {
            AsUserA();
            var meetingId = Guid.NewGuid();
            _meetingsMock.Setup(r => r.GetAsync(_userA, meetingId))
                .ReturnsAsync(new Meeting
                {
                    MeetingId = meetingId,
                    ApplicationUserId = _userA,
                    Title = "Planned sync",
                    Agenda = "Discuss roadmap",
                    Description = "Quarterly planning"
                });

            await Assert.ThrowsAsync<InvalidOperationException>(() => Service().SubmitForMeetingAsync(meetingId));
            _batchesMock.Verify(r => r.AddBatchAsync(It.IsAny<IngestionBatch>()), Times.Never);
        }

        [Fact]
        public async Task SubmitForMeetingAsync_UsesActualEvidenceOnly()
        {
            AsUserA();
            var meetingId = Guid.NewGuid();
            _meetingsMock.Setup(r => r.GetAsync(_userA, meetingId))
                .ReturnsAsync(new Meeting
                {
                    MeetingId = meetingId,
                    ApplicationUserId = _userA,
                    Title = "Planned sync",
                    Agenda = "SECRET-AGENDA-MUST-NOT-INGEST",
                    RawTranscript = "We agreed to meet monthly.",
                    RawNotes = "Follow up on hiring."
                });
            _batchesMock.Setup(r => r.FindByHashAsync(_userA, IngestionSourceType.MeetingText, meetingId, It.IsAny<string>()))
                .ReturnsAsync((IngestionBatch?)null);
            IngestionBatch? saved = null;
            _batchesMock.Setup(r => r.AddBatchAsync(It.IsAny<IngestionBatch>()))
                .Callback<IngestionBatch>(b => saved = b)
                .Returns(Task.CompletedTask);
            var batch = Batch(_userA);
            _batchesMock.Setup(r => r.GetAsync(_userA, It.IsAny<Guid>())).ReturnsAsync(batch);

            await Service().SubmitForMeetingAsync(meetingId);

            saved.Should().NotBeNull();
            saved!.RawText.Should().Contain("We agreed to meet monthly.");
            saved.RawText.Should().NotContain("SECRET-AGENDA-MUST-NOT-INGEST");
            saved.SourceMeetingId.Should().Be(meetingId);
        }

        [Fact]
        public async Task ReviewAsync_ApproveThenEditedTitle_StaysReviewed()
        {
            AsUserA();
            var batchId = Guid.NewGuid();
            var finding = new IngestionFinding
            {
                IngestionFindingId = Guid.NewGuid(),
                IngestionBatchId = batchId,
                ApplicationUserId = _userA,
                Title = "Original",
                Status = IngestionFindingStatus.Pending,
                Confidence = FindingConfidence.High
            };
            var batch = Batch(_userA, batchId);
            batch.Findings.Add(finding);
            _batchesMock.Setup(r => r.GetFindingAsync(_userA, finding.IngestionFindingId)).ReturnsAsync(finding);
            _batchesMock.Setup(r => r.GetAsync(_userA, batchId)).ReturnsAsync(batch);

            var approved = await Service().ReviewAsync(new IngestionFindingReviewRequest
            {
                FindingId = finding.IngestionFindingId,
                Status = IngestionFindingStatus.Approved
            });
            approved.Status.Should().Be(IngestionFindingStatus.Approved.ToString());

            var edited = await Service().ReviewAsync(new IngestionFindingReviewRequest
            {
                FindingId = finding.IngestionFindingId,
                Status = IngestionFindingStatus.Pending,
                Title = "Corrected title"
            });
            // A corrected title is still reviewed work: it must stay in the
            // Approve-person/batch set, never silently drop to unreviewed.
            edited.Status.Should().Be(IngestionFindingStatus.Edited.ToString());
            edited.Title.Should().Be("Corrected title");
        }

        [Fact]
        public async Task GetAsync_OtherOwnersBatch_NotFound()
        {
            _userMock.Setup(u => u.UserId).Returns(_userB);
            _batchesMock.Setup(r => r.GetAsync(_userB, It.IsAny<Guid>()))
                .ReturnsAsync((IngestionBatch?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => Service().GetAsync(Guid.NewGuid()));
        }
    }
}
