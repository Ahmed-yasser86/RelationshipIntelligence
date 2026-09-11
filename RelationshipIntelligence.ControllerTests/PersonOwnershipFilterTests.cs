using Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Moq;
using RepositryContracts;
using ServiceContracts;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace CRUDTests.ControllersTest
{
    public class PersonOwnershipFilterTests
    {
        private readonly Guid _userA = Guid.NewGuid();
        private readonly Mock<PersonRepositryContract> _personsMock = new();
        private readonly Mock<ICurrentUserService> _userMock = new();

        private PersonOwnershipFilter Filter()
        {
            return new PersonOwnershipFilter(_personsMock.Object, _userMock.Object);
        }

        private static ActionExecutingContext ContextWithId(Guid? id)
        {
            var http = new DefaultHttpContext();
            var arguments = new Dictionary<string, object?>();
            if (id.HasValue)
                arguments["id"] = id.Value;

            return new ActionExecutingContext(
                new ActionContext(http, new RouteData(), new ActionDescriptor()),
                new List<IFilterMetadata>(),
                arguments,
                new object());
        }

        [Fact]
        public async Task MissingIdArgument_LetsRequestThrough()
        {
            bool nextCalled = false;
            var context = ContextWithId(null);

            await Filter().OnActionExecutionAsync(context, () =>
            {
                nextCalled = true;
                return Task.FromResult<ActionExecutedContext>(null!);
            });

            nextCalled.Should().BeTrue();
            context.Result.Should().BeNull();
        }

        [Fact]
        public async Task UnauthenticatedUser_ReturnsUnauthorized()
        {
            _userMock.Setup(u => u.UserId).Returns((Guid?)null);
            var context = ContextWithId(Guid.NewGuid());

            await Filter().OnActionExecutionAsync(context, () =>
                Task.FromResult<ActionExecutedContext>(null!));

            context.Result.Should().BeOfType<UnauthorizedResult>();
        }

        [Fact]
        public async Task ForeignPerson_ReturnsForbid()
        {
            _userMock.Setup(u => u.UserId).Returns(_userA);
            var foreignId = Guid.NewGuid();
            _personsMock.Setup(r => r.GetPersonById(foreignId))
                .ReturnsAsync((Person?)null);

            var context = ContextWithId(foreignId);

            await Filter().OnActionExecutionAsync(context, () =>
                Task.FromResult<ActionExecutedContext>(null!));

            context.Result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task OwnedPerson_LetsRequestThrough()
        {
            _userMock.Setup(u => u.UserId).Returns(_userA);
            var personId = Guid.NewGuid();
            _personsMock.Setup(r => r.GetPersonById(personId))
                .ReturnsAsync(new Person { PersonId = personId, ApplicationUserId = _userA, Name = "Own" });

            var context = ContextWithId(personId);
            bool nextCalled = false;

            await Filter().OnActionExecutionAsync(context, () =>
            {
                nextCalled = true;
                return Task.FromResult<ActionExecutedContext>(null!);
            });

            nextCalled.Should().BeTrue();
            context.HttpContext.Items["ValidatedPerson"].Should().NotBeNull();
        }
    }
}
