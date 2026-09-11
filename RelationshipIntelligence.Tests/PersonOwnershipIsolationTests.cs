using ContactsManger.Core.Domain.IdentityEntities;
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
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CRUDTests
{
    public class PersonOwnershipIsolationTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly Guid _userA = Guid.NewGuid();
        private readonly Guid _userB = Guid.NewGuid();
        private readonly Guid _personAId = Guid.NewGuid();
        private readonly Guid _personBId = Guid.NewGuid();

        public PersonOwnershipIsolationTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            using var seed = ContextFor(_userA);
            seed.Database.EnsureCreated();
            seed.Users.Add(new ApplicationUser { Id = _userA, UserName = "a@test.com" });
            seed.Users.Add(new ApplicationUser { Id = _userB, UserName = "b@test.com" });
            seed.Persons.Add(new Person
            {
                PersonId = _personAId,
                Name = "Owner A Person",
                ApplicationUserId = _userA
            });
            seed.Persons.Add(new Person
            {
                PersonId = _personBId,
                Name = "Owner B Person",
                ApplicationUserId = _userB
            });
            seed.SaveChanges();
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

        private PersonRepository RepositoryFor(Guid? userId)
        {
            return new PersonRepository(ContextFor(userId));
        }

        [Fact]
        public async Task GetAllPersons_ReturnsOnlyCurrentOwnersRows()
        {
            using var db = ContextFor(_userA);
            var repository = new PersonRepository(db);

            var persons = (await repository.GetAllPersons()).ToList();

            persons.Should().ContainSingle(p => p.PersonId == _personAId);
            persons.Should().NotContain(p => p.PersonId == _personBId);
        }

        [Fact]
        public async Task GetPersonById_ForeignPerson_ReturnsNull()
        {
            var repository = RepositoryFor(_userA);

            (await repository.GetPersonById(_personBId)).Should().BeNull();
        }

        [Fact]
        public async Task GetFilteredPersons_ForeignPersonExcluded()
        {
            var repository = RepositoryFor(_userB);

            var persons = await repository.GetFilteredPersons(p => p.Name.Contains("Person"));

            persons.Should().ContainSingle(p => p.PersonId == _personBId);
        }

        [Fact]
        public async Task DeletePerson_ForeignPerson_ReturnsFalseAndKeepsRow()
        {
            var repository = RepositoryFor(_userA);

            (await repository.DeletePerson(_personBId)).Should().BeFalse();

            using var verify = ContextFor(_userB);
            (await verify.Persons.FindAsync(_personBId)).Should().NotBeNull();
        }

        [Fact]
        public async Task UnauthenticatedUser_SeesNothing()
        {
            var repository = RepositoryFor(null);

            (await repository.GetAllPersons()).Should().BeEmpty();
            (await repository.GetPersonById(_personAId)).Should().BeNull();
        }

        public void Dispose()
        {
            _connection.Close();
            _connection.Dispose();
        }
    }
}
