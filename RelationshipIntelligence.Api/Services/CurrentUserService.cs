using Microsoft.AspNetCore.Http;
using System;
using System.Security.Claims;

namespace RelationshipIntelligence.Api.Services
{
    public class CurrentUserService : ServiceContracts.ICurrentUserService
    {
        public Guid? UserId { get; }

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            var idClaim = httpContextAccessor.HttpContext?.User
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;
            UserId = Guid.TryParse(idClaim, out var id) ? id : null;
        }
    }
}
