using Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RepositryContracts;
using ServiceContracts;
using ServiceContracts.DTOs.MemoryDTOs;
using Servicess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CRUDTests
{
    public class RelationshipMemoryServiceTests
    {
        private readonly Guid _userA = Guid.NewGuid();
        private readonly Guid _userB = Guid.NewGuid();
        private readonly Mock<RelationshipMemoryRepositoryContract> _entriesMock = new();
        private readonly Mock<PersonRepositryContract> _personsMock = new();
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<ICurrentUserService> _userMock = new();

        private RelationshipMemoryService Service() => new(
            _entriesMock.Object,
            _personsMock.Object,
            _uowMock.Object,
            _userMock.Object,
            Mock.Of<ILogger<RelationshipMemoryService>>());

        private Person OwnedPerson(Guid owner, Guid? personId = null) => new()
        {
            PersonId = personId ?? Guid.NewGuid(),
            ApplicationUserId = owner,
            Name = "Test Person"
        };

        [Fact]
        public async Task CreateAsync_OtherOwnersPerson_ThrowsNotFound()
        {
            var personId = Guid.NewGuid();
            _userMock.Setup(u => u.UserId).Returns(_userA);
            _personsMock.Setup(r => r.GetPersonById(personId))
                .ReturnsAsync(OwnedPerson(_userB, personId));

            await Assert.ThrowsAsync<KeyNotFoundException>(() => Service().CreateAsync(new MemoryEntryCreateRequest
            {
                PersonId = personId,
                Kind = RelationshipMemoryKind.Fact,
                Title = "Should not be created"
            }));
            _uowMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_BlankTitle_Throws()
        {
            var personId = Guid.NewGuid();
            _userMock.Setup(u => u.UserId).Returns(_userA);
            _personsMock.Setup(r => r.GetPersonById(personId)).ReturnsAsync(OwnedPerson(_userA, personId));

            await Assert.ThrowsAsync<ArgumentException>(() => Service().CreateAsync(new MemoryEntryCreateRequest
            {
                PersonId = personId,
                Kind = RelationshipMemoryKind.Goal,
                Title = "   "
            }));
        }

        [Fact]
        public async Task UpdateAsync_AiDerivedEntry_FlipsProvenanceToUserWithCorrectionTrail()
        {
            var entryId = Guid.NewGuid();
            var entry = new RelationshipMemoryEntry
            {
                MemoryEntryId = entryId,
                ApplicationUserId = _userA,
                PersonId = Guid.NewGuid(),
                Kind = RelationshipMemoryKind.Fact,
                Title = "Wrong role",
                Provenance = MemoryProvenance.MeetingDerived,
                Status = MemoryEntryStatus.Active,
                CreatedAtUtc = DateTime.UtcNow
            };
            _userMock.Setup(u => u.UserId).Returns(_userA);
            _entriesMock.Setup(r => r.GetAsync(_userA, entryId)).ReturnsAsync(entry);

            var result = await Service().UpdateAsync(new MemoryEntryUpdateRequest
            {
                MemoryEntryId = entryId,
                Kind = RelationshipMemoryKind.Fact,
                Title = "Correct role",
                Status = MemoryEntryStatus.Active,
                CorrectionNote = "User corrected after review"
            });

            result.Provenance.Should().Be(MemoryProvenance.User);
            result.Title.Should().Be("Correct role");
            entry.CorrectedAtUtc.Should().NotBeNull();
            entry.CorrectionNote.Should().Be("User corrected after review");
            _uowMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_OtherOwnerEntry_ThrowsNotFound()
        {
            var entryId = Guid.NewGuid();
            _userMock.Setup(u => u.UserId).Returns(_userA);
            _entriesMock.Setup(r => r.GetAsync(_userA, entryId))
                .ReturnsAsync((RelationshipMemoryEntry?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => Service().DeleteAsync(entryId));
            _uowMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task AcceptSuggestionAsync_NonSuggested_Throws()
        {
            var entryId = Guid.NewGuid();
            _userMock.Setup(u => u.UserId).Returns(_userA);
            _entriesMock.Setup(r => r.GetAsync(_userA, entryId)).ReturnsAsync(new RelationshipMemoryEntry
            {
                MemoryEntryId = entryId,
                ApplicationUserId = _userA,
                PersonId = Guid.NewGuid(),
                Kind = RelationshipMemoryKind.Topic,
                Title = "User fact",
                Provenance = MemoryProvenance.User,
                Status = MemoryEntryStatus.Active
            });

            await Assert.ThrowsAsync<InvalidOperationException>(() => Service().AcceptSuggestionAsync(entryId));
        }

        [Fact]
        public async Task AcceptSuggestionAsync_Suggested_BecomesConfirmed()
        {
            var entryId = Guid.NewGuid();
            var entry = new RelationshipMemoryEntry
            {
                MemoryEntryId = entryId,
                ApplicationUserId = _userA,
                PersonId = Guid.NewGuid(),
                Kind = RelationshipMemoryKind.Topic,
                Title = "Suggested topic",
                Provenance = MemoryProvenance.AiSuggested,
                Status = MemoryEntryStatus.Active
            };
            _userMock.Setup(u => u.UserId).Returns(_userA);
            _entriesMock.Setup(r => r.GetAsync(_userA, entryId)).ReturnsAsync(entry);

            var result = await Service().AcceptSuggestionAsync(entryId);

            result.Provenance.Should().Be(MemoryProvenance.AiConfirmed);
            _uowMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task ListForPersonAsync_ReturnsOnlyCallerEntries()
        {
            var personId = Guid.NewGuid();
            _userMock.Setup(u => u.UserId).Returns(_userA);
            _personsMock.Setup(r => r.GetPersonById(personId)).ReturnsAsync(OwnedPerson(_userA, personId));
            _entriesMock.Setup(r => r.ListForPersonAsync(_userA, personId)).ReturnsAsync(new List<RelationshipMemoryEntry>
            {
                new() { MemoryEntryId = Guid.NewGuid(), ApplicationUserId = _userA, PersonId = personId, Kind = RelationshipMemoryKind.Goal, Title = "Reconnect", Provenance = MemoryProvenance.User, Status = MemoryEntryStatus.Active }
            }.AsEnumerable().ToList());

            var result = await Service().ListForPersonAsync(personId);

            result.Should().ContainSingle().Which.Title.Should().Be("Reconnect");
        }
    }
}
