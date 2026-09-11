using ContactsManger.Core.Domain.IdentityEntities;
using Entities;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using RepositryContracts;
using ServiceContracts;
using ServiceContracts.DTOs;
using Servicess;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace CRUDTests
{
    public class OwnerIsolationTests
    {
        private readonly Guid _userA = Guid.NewGuid();
        private readonly Mock<ICurrentUserService> _userMock = new();

        private Mock<ICurrentUserService> ActingAs(Guid userId)
        {
            _userMock.Setup(u => u.UserId).Returns(userId);
            return _userMock;
        }

        [Fact]
        public void JwtAuthenticate_NameIdentifierIsUserId()
        {
            var configValues = new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "this-is-a-test-signing-key-with-enough-length-123456",
                ["Jwt:issuer"] = "test-issuer",
                ["Jwt:audience"] = "test-audience",
                ["Jwt:expiration_minutes"] = "60",
                ["RefreshToken:expiration_minutes"] = "1440"
            };
            var config = new Mock<IConfiguration>();
            config.Setup(c => c[It.IsAny<string>()])
                .Returns<string>(key => configValues.TryGetValue(key, out var value) ? value : null);

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "owner@example.com",
                PersonName = "Owner"
            };

            var response = new ContactsManger.Core.Services.JwtServices(config.Object).Authenticate(user);
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(response.token);

            token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value
                .Should().Be(user.Id.ToString());
            token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value
                .Should().Be(user.Email);
        }

        [Fact]
        public async Task QuickAdd_AttachesPersonThroughRepository()
        {
            var personsMock = new Mock<PersonRepositryContract>();
            var circlesMock = new Mock<CircleRepositryContract>();
            Person? saved = null;
            personsMock.Setup(r => r.AddPerson(It.IsAny<Person>()))
                .Callback<Person>(p => saved = p)
                .ReturnsAsync((Person p) => p);

            var service = new PersonQuickAdderService(
                personsMock.Object,
                circlesMock.Object,
                ActingAs(_userA).Object,
                Mock.Of<IUnitOfWork>(),
                Mock.Of<ILogger<PersonQuickAdderService>>());

            var result = await service.QuickAddPerson(new PersonQuickAddRequest
            {
                Name = "New Contact",
                email = "new@example.com"
            });

            personsMock.Verify(r => r.AddPerson(It.IsAny<Person>()), Times.Once);
            saved.Should().NotBeNull();
            saved!.ApplicationUserId.Should().Be(_userA);
            result.PersonId.Should().Be(saved.PersonId);
        }

        [Fact]
        public async Task GetPersonByPersonId_ForeignPerson_ReturnsNull()
        {
            var personsMock = new Mock<PersonRepositryContract>();
            var foreignId = Guid.NewGuid();
            personsMock.Setup(r => r.GetPersonById(foreignId)).ReturnsAsync((Person?)null);

            var service = new PersonGetterService(
                personsMock.Object, Mock.Of<ILogger<PersonGetterService>>());

            (await service.GetPersonByPersonId(foreignId)).Should().BeNull();
        }

        [Fact]
        public async Task UpdatePerson_ForeignPerson_ThrowsArgumentException()
        {
            var personsMock = new Mock<PersonRepositryContract>();
            personsMock.Setup(r => r.GetPersonById(It.IsAny<Guid?>())).ReturnsAsync((Person?)null);

            var service = new PersonUpdaterService(
                personsMock.Object,
                Mock.Of<CircleRepositryContract>(),
                Mock.Of<ContactItemRoleRepositryContract>(),
                Mock.Of<ConnectionChannelRepositryContract>(),
                Mock.Of<UserDefinedTagsRepositryContract>(),
                Mock.Of<SystemStatusTagRepositryContract>(),
                Mock.Of<ILogger<PersonUpdaterService>>(),
                Mock.Of<SocialMediaAccountRepositryContract>(),
                Mock.Of<IUnitOfWork>());

            await Assert.ThrowsAsync<ArgumentException>(() => service.UpdatePerson(
                new PersonUpdateRequest { PersonId = Guid.NewGuid(), Name = "X" }));
        }

        [Fact]
        public async Task DeletePerson_ForeignPerson_ReturnsFalse()
        {
            var personsMock = new Mock<PersonRepositryContract>();
            personsMock.Setup(r => r.DeletePerson(It.IsAny<Guid?>())).ReturnsAsync(false);

            var service = new PersonDeleterService(
                Mock.Of<IUnitOfWork>(), personsMock.Object,
                Mock.Of<ILogger<PersonDeleterService>>());

            (await service.DeletePersonByPersonId(Guid.NewGuid())).Should().BeFalse();
        }
    }
}
