using Entities;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Repositories;
using RepositryContracts;
using ServiceContracts;
using System;
using System.Threading.Tasks;
using Xunit;

namespace CRUDTests
{
    public class RelationshipStateRepositoryTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly Guid _userA = Guid.NewGuid();

        public RelationshipStateRepositoryTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
        }

        private AppDBContext ContextFor(Guid? userId)
        {
            var userMock = new Mock<ICurrentUserService>();
            userMock.Setup(u => u.UserId).Returns(userId);
            var options = new DbContextOptionsBuilder<AppDBContext>()
                .UseSqlite(_connection)
                .Options;
            return new AppDBContext(options, userMock.Object);
        }

        [Fact]
        public async Task UpsertAsync_UpdatePath_PersistsAllScoringFields()
        {
            using var db = ContextFor(_userA);
            db.Database.EnsureCreated();
            var personId = Guid.NewGuid();
            var repository = new RelationshipStateRepository(
                db, Mock.Of<ILogger<RelationshipStateRepository>>());

            await repository.UpsertAsync(new RelationshipState
            {
                ApplicationUserId = _userA,
                PersonId = personId,
                TieStrength = 0.5,
                UrgencyScore = 10,
                InteractionCount = 0,
                EvidenceStatus = EvidenceStatus.NoHistory,
                UpdatedAtUtc = DateTime.UtcNow.AddDays(-1)
            });
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            await repository.UpsertAsync(new RelationshipState
            {
                ApplicationUserId = _userA,
                PersonId = personId,
                TieStrength = 1.2,
                UrgencyScore = 60,
                InteractionCount = 3,
                EvidenceStatus = EvidenceStatus.Insufficient,
                LastContactAtUtc = DateTime.UtcNow.AddDays(-9),
                CadenceReferenceDays = 30,
                UpdatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            var reloaded = await repository.GetAsync(_userA, personId);
            reloaded.Should().NotBeNull();
            reloaded!.TieStrength.Should().BeApproximately(1.2, 1e-9);
            reloaded.UrgencyScore.Should().Be(60);
            reloaded.InteractionCount.Should().Be(3);
            reloaded.EvidenceStatus.Should().Be(EvidenceStatus.Insufficient);
            reloaded.LastContactAtUtc.Should().NotBeNull();
        }

        public void Dispose()
        {
            _connection.Close();
            _connection.Dispose();
        }
    }
}
