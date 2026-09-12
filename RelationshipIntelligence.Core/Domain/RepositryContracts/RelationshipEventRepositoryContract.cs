using Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RepositryContracts
{
    public interface RelationshipEventRepositoryContract
    {
        Task<List<RelationshipEvent>> ListForPersonAsync(Guid ownerId, Guid personId);

        Task<List<RelationshipEvent>> ListForOwnerAsync(Guid ownerId);

        Task<RelationshipEvent?> GetAsync(Guid ownerId, Guid eventId);

        Task AddAsync(RelationshipEvent relationshipEvent);

        Task RemoveAsync(RelationshipEvent relationshipEvent);
    }
}
