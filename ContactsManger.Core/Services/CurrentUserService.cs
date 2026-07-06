using System;
using Microsoft.AspNetCore.Http;
namespace Servicess
{
    public class CurrentUserService : ServiceContracts.ICurrentUserService
    {
        public Guid? UserId { get; }
        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            var idClaim = httpContextAccessor.HttpContext?.User
                .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            UserId = Guid.TryParse(idClaim, out var id) ? id : null;
        }
    }
}