using ServiceContracts;
using System;

namespace RelationshipIntelligence.Api.Workers
{
    internal sealed class FixedUserService : ICurrentUserService
    {
        public FixedUserService(Guid? userId)
        {
            UserId = userId;
        }

        public Guid? UserId { get; }
    }
}
