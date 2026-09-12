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
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CRUDTests
{
    public class DemoWorkspaceServiceTests
    {
        private readonly Guid _userA = Guid.NewGuid();
        private readonly Mock<IPersonQuickAdderService> _quickAddMock = new();
        private readonly Mock<IInteractionService> _interactionsMock = new();
        private readonly Mock<PersonRepositryContract> _personsMock = new();
        private readonly Mock<IPersonDeleterService> _deleterMock = new();
        private readonly Mock<ICurrentUserService> _userMock = new();

        private DemoWorkspaceService Service() => new(
            _quickAddMock.Object,
            _interactionsMock.Object,
            _personsMock.Object,
            _deleterMock.Object,
            _userMock.Object,
            Mock.Of<ILogger<DemoWorkspaceService>>());

        [Fact]
        public async Task SeedAsync_NonEmptyWorkspace_Throws()
        {
            _userMock.Setup(u => u.UserId).Returns(_userA);
            _personsMock.Setup(r => r.GetAllPersons()).ReturnsAsync(
                Enumerable.Range(0, 5).Select(_ => new Person()).AsEnumerable());

            await Assert.ThrowsAsync<InvalidOperationException>(() => Service().SeedAsync());
            _quickAddMock.Verify(q => q.QuickAddPerson(It.IsAny<PersonQuickAddRequest>()), Times.Never);
        }

        [Fact]
        public async Task SeedAsync_Unauthenticated_Throws()
        {
            _userMock.Setup(u => u.UserId).Returns((Guid?)null);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Service().SeedAsync());
        }

        [Fact]
        public async Task SeedAsync_EmptyWorkspace_CreatesEightContacts()
        {
            _userMock.Setup(u => u.UserId).Returns(_userA);
            _personsMock.Setup(r => r.GetAllPersons()).ReturnsAsync(new List<Person>().AsEnumerable());
            _quickAddMock.Setup(q => q.QuickAddPerson(It.IsAny<PersonQuickAddRequest>()))
                .ReturnsAsync((PersonQuickAddRequest req) => new PersonRespones
                {
                    PersonId = Guid.NewGuid(),
                    Name = req.Name ?? string.Empty
                });
            _interactionsMock.Setup(i => i.LogAsync(It.IsAny<InteractionAddRequest>()))
                .ReturnsAsync(new InteractionResponse());

            var count = await Service().SeedAsync();

            count.Should().Be(8);
            _quickAddMock.Verify(q => q.QuickAddPerson(It.IsAny<PersonQuickAddRequest>()), Times.Exactly(8));
        }

        [Fact]
        public async Task ClearAsync_RemovesOnlyDemoContacts()
        {
            _userMock.Setup(u => u.UserId).Returns(_userA);
            var demoId = Guid.NewGuid();
            var realId = Guid.NewGuid();
            _personsMock.Setup(r => r.GetAllPersons()).ReturnsAsync(new List<Person>
            {
                new() { PersonId = demoId, ApplicationUserId = _userA, Name = "Maya Chen", email = "maya.chen@example.com" },
                new() { PersonId = realId, ApplicationUserId = _userA, Name = "Real Client", email = "real@client.com" }
            }.AsEnumerable());
            _deleterMock.Setup(d => d.DeletePersonByPersonId(demoId)).ReturnsAsync(true);

            var removed = await Service().ClearAsync();

            removed.Should().Be(1);
            _deleterMock.Verify(d => d.DeletePersonByPersonId(realId), Times.Never);
        }
    }
}
