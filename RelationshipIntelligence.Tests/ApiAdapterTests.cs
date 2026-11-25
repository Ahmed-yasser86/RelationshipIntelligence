using FluentAssertions;
using Microsoft.AspNetCore.Http;
using RelationshipIntelligence.Api.Services;
using System;
using System.Security.Claims;

namespace CRUDTests
{
    public class ApiAdapterTests
    {
        [Fact]
        public void CurrentUserService_ReadsOwnerIdFromNameIdentifierClaim()
        {
            var userId = Guid.NewGuid();
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "test"));
            var accessor = new HttpContextAccessor { HttpContext = httpContext };

            var service = new CurrentUserService(accessor);

            service.UserId.Should().Be(userId);
        }
    }
}
