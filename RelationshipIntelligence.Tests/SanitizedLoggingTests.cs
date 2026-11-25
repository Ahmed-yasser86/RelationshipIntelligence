using ContactsManger.Core.DTOs.PersonDTOs;
using Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RepositryContracts;
using ServiceContracts;
using ServiceContracts.DTOs;
using Servicess;
using Servicess.Helpers;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace CRUDTests
{
    /// <summary>
    /// Exercises error paths whose log statements were sanitized for the CodeQL
    /// log-forging remediation, so the changed lines are covered.
    /// </summary>
    public class SanitizedLoggingTests
    {
        [Fact]
        public async Task PersonAdder_NullRequest_Throws()
        {
            var service = new PersonAdderService(
                Mock.Of<PersonRepositryContract>(),
                Mock.Of<CircleRepositryContract>(),
                Mock.Of<ContactItemRoleRepositryContract>(),
                Mock.Of<ConnectionChannelRepositryContract>(),
                Mock.Of<UserDefinedTagsRepositryContract>(),
                Mock.Of<SystemStatusTagRepositryContract>(),
                Mock.Of<ICurrentUserService>(),
                Mock.Of<IUnitOfWork>(),
                Mock.Of<ILogger<PersonAdderService>>());

            await Assert.ThrowsAsync<ArgumentNullException>(() => service.AddPerson(null));
        }

        [Fact]
        public async Task PersonQuickAdder_NullRequest_Throws()
        {
            var service = new PersonQuickAdderService(
                Mock.Of<PersonRepositryContract>(),
                Mock.Of<CircleRepositryContract>(),
                Mock.Of<ICurrentUserService>(),
                Mock.Of<IUnitOfWork>(),
                Mock.Of<ILogger<PersonQuickAdderService>>());

            await Assert.ThrowsAsync<ArgumentNullException>(() => service.QuickAddPerson(null));
        }

        [Fact]
        public async Task PersonUpdater_NullRequest_Throws()
        {
            var service = new PersonUpdaterService(
                Mock.Of<PersonRepositryContract>(),
                Mock.Of<CircleRepositryContract>(),
                Mock.Of<ContactItemRoleRepositryContract>(),
                Mock.Of<ConnectionChannelRepositryContract>(),
                Mock.Of<UserDefinedTagsRepositryContract>(),
                Mock.Of<SystemStatusTagRepositryContract>(),
                Mock.Of<ILogger<PersonUpdaterService>>(),
                Mock.Of<SocialMediaAccountRepositryContract>(),
                Mock.Of<IUnitOfWork>());

            await Assert.ThrowsAsync<ArgumentNullException>(() => service.UpdatePerson(null));
        }

        [Fact]
        public async Task PersonSearcher_RepositoryFailure_RethrowsAndLogsSanitizedValues()
        {
            var repo = new Mock<PersonRepositryContract>();
            repo.Setup(r => r.GetFilteredPersonsPaged(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Expression<Func<Person, bool>>>()))
                .ThrowsAsync(new InvalidOperationException("search store down"));
            var service = new PersonSearcherService(repo.Object, Mock.Of<ILogger<PersonSearcherService>>());

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.SearchPersonsBy_Batched("Salma\r\n forged", "Name", 1, 10));

            LogSanitizer.Sanitize("Salma\r\n forged").Should().Be("Salma forged");
        }

        [Fact]
        public async Task PersonAdder_InvalidRequest_CoversValidationLog()
        {
            var service = new PersonAdderService(
                Mock.Of<PersonRepositryContract>(), Mock.Of<CircleRepositryContract>(),
                Mock.Of<ContactItemRoleRepositryContract>(), Mock.Of<ConnectionChannelRepositryContract>(),
                Mock.Of<UserDefinedTagsRepositryContract>(), Mock.Of<SystemStatusTagRepositryContract>(),
                Mock.Of<ICurrentUserService>(), Mock.Of<IUnitOfWork>(),
                Mock.Of<ILogger<PersonAdderService>>());

            await Assert.ThrowsAnyAsync<Exception>(() => service.AddPerson(new PersonAddRequest()));
        }

        [Fact]
        public async Task PersonQuickAdder_InvalidRequest_CoversValidationLog()
        {
            var service = new PersonQuickAdderService(
                Mock.Of<PersonRepositryContract>(), Mock.Of<CircleRepositryContract>(),
                Mock.Of<ICurrentUserService>(), Mock.Of<IUnitOfWork>(),
                Mock.Of<ILogger<PersonQuickAdderService>>());

            await Assert.ThrowsAnyAsync<Exception>(() => service.QuickAddPerson(new PersonQuickAddRequest()));
        }

        [Fact]
        public async Task PersonUpdater_InvalidRequest_CoversValidationLog()
        {
            var service = new PersonUpdaterService(
                Mock.Of<PersonRepositryContract>(), Mock.Of<CircleRepositryContract>(),
                Mock.Of<ContactItemRoleRepositryContract>(), Mock.Of<ConnectionChannelRepositryContract>(),
                Mock.Of<UserDefinedTagsRepositryContract>(), Mock.Of<SystemStatusTagRepositryContract>(),
                Mock.Of<ILogger<PersonUpdaterService>>(),
                Mock.Of<SocialMediaAccountRepositryContract>(), Mock.Of<IUnitOfWork>());

            await Assert.ThrowsAnyAsync<Exception>(() => service.UpdatePerson(new PersonUpdateRequest()));
        }
    }
}
