using ServiceContracts;
using System;

namespace RelationshipIntelligence.Infrastructure.Services
{
    /// <summary>
    /// Background-only identity used by maintenance work; it is never registered
    /// in the HTTP request DI graph.
    /// </summary>
    public sealed class FixedUserService : ICurrentUserService
    {
        public FixedUserService(Guid? userId)
        {
            UserId = userId;
        }

        public Guid? UserId { get; }
    }
}
