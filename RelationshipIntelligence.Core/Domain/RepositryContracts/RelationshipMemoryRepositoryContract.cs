using Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RepositryContracts
{
    public interface RelationshipMemoryRepositoryContract
    {
        Task<List<RelationshipMemoryEntry>> ListForPersonAsync(Guid ownerId, Guid personId);

        Task<RelationshipMemoryEntry?> GetAsync(Guid ownerId, Guid entryId);

        Task AddAsync(RelationshipMemoryEntry entry);

        Task RemoveAsync(RelationshipMemoryEntry entry);
    }
}
