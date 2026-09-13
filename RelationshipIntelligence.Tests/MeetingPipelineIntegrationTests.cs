using Entities;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Repositories;
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
    /// <summary>
    /// SQLite-backed integration coverage for the meeting pipeline. Guards the
    /// fixed persistence pattern: new child rows on already-tracked meetings
    /// must be inserted explicitly, never depend on ambiguous cascade tracking.
    /// </summary>
    public class MeetingPipelineIntegrationTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly Guid _userA = Guid.NewGuid();
        private readonly Guid _salmaId = Guid.NewGuid();

        public MeetingPipelineIntegrationTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
        }

        public void Dispose()
        {
            _connection.Close();
            _connection.Dispose();
        }

        private AppDBContext NewContext()
        {
            var userMock = new Mock<ICurrentUserService>();
            userMock.Setup(u => u.UserId).Returns(_userA);
            var options = new DbContextOptionsBuilder<AppDBContext>()
                .UseSqlite(_connection)
                .Options;
            return new AppDBContext(options, userMock.Object);
        }

        private async Task<Guid> SeedMeetingAsync()
        {
            using var db = NewContext();
            db.Database.EnsureCreated();
            db.Users.Add(new ContactsManger.Core.Domain.IdentityEntities.ApplicationUser
            {
                Id = _userA,
                UserName = $"repro-{_userA:N}@example.com",
                NormalizedUserName = $"REPRO-{_userA:N}@EXAMPLE.COM",
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString()
            });
            db.Persons.Add(new Person
            {
                PersonId = _salmaId,
                ApplicationUserId = _userA,
                Name = "Salma El-Sayed"
            });
            var meetingId = Guid.NewGuid();
            db.Meetings.Add(new Meeting
            {
                MeetingId = meetingId,
                ApplicationUserId = _userA,
                Title = "Debug",
                OccurredAtUtc = DateTime.UtcNow,
                Status = MeetingStatus.Draft,
                RawNotes = "Salma El-Sayed joined. She will send the notes.",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
            return meetingId;
        }

        private MeetingService ServiceFor(AppDBContext db, Mock<ICurrentUserService> userMock)
        {
            var uow = new UnitOfWork(db);
            var meetings = new MeetingRepository(db);
            var memoryRepo = new RelationshipMemoryRepository(db, Mock.Of<ILogger<RelationshipMemoryRepository>>());
            var personsMock = new Mock<PersonRepositryContract>();
            personsMock.Setup(r => r.GetAllPersons()).ReturnsAsync(new List<Person>
            {
                new() { PersonId = _salmaId, ApplicationUserId = _userA, Name = "Salma El-Sayed" }
            }.AsEnumerable());
            personsMock.Setup(r => r.GetPersonById(_salmaId)).ReturnsAsync(
                new Person { PersonId = _salmaId, ApplicationUserId = _userA, Name = "Salma El-Sayed" });
            var memoryService = new RelationshipMemoryService(
                memoryRepo, personsMock.Object, uow, userMock.Object,
                Mock.Of<ILogger<RelationshipMemoryService>>());
            var eventsService = new EventService(
                Mock.Of<RelationshipEventRepositoryContract>(), personsMock.Object, uow, userMock.Object,
                Mock.Of<ILogger<EventService>>());
            return new MeetingService(
                meetings, memoryRepo, personsMock.Object,
                Mock.Of<IInteractionService>(), memoryService, eventsService,
                Mock.Of<IRelationshipScoringService>(),
                new RelationshipIntelligence.AI.StubMeetingExtractor(),
                uow, userMock.Object, Mock.Of<ILogger<MeetingService>>());
        }

        [Fact]
        public async Task ProcessAsync_PersistsPeopleAndFindingsAsNewRows()
        {
            var meetingId = await SeedMeetingAsync();
            using var db = NewContext();
            var userMock = new Mock<ICurrentUserService>();
            userMock.Setup(u => u.UserId).Returns(_userA);

            var result = await ServiceFor(db, userMock).ProcessAsync(meetingId);

            result.Status.Should().Be(MeetingStatus.Processed);
            var names = string.Join(" | ", result.People.Select(p => $"{p.DetectedName}/{p.MatchStatus}/{p.MappedPersonId}"));
            result.People.Where(p => p.DetectedName == "Salma El-Sayed").Should().ContainSingle($"but got: {names}");

            using var verify = NewContext();
            var dbPeople = await verify.MeetingPersons.Where(p => p.MeetingId == meetingId).ToListAsync();
            var dbInfo = string.Join(" | ", dbPeople.Select(p => $"{p.DetectedName}/{p.MeetingPersonId}"));
            dbPeople.Should().ContainSingle($"db has: {dbInfo}");
        }
    }
}
