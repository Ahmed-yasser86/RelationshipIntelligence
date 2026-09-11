using Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RepositryContracts
{
    public interface RelationshipStateRepositoryContract
    {
        Task UpsertAsync(RelationshipState state);

        Task<List<RelationshipState>> ListForOwnerAsync(Guid ownerId);

        Task<RelationshipState?> GetAsync(Guid ownerId, Guid personId);
    }
}
