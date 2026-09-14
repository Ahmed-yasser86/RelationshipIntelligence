using Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RepositryContracts
{
    public interface RelationshipPreferenceRepositoryContract
    {
        Task<RelationshipPreference?> GetAsync(Guid ownerId, Guid personId);

        Task<List<RelationshipPreference>> ListForOwnerAsync(Guid ownerId);

        Task AddAsync(RelationshipPreference preference);

        Task RemoveAsync(RelationshipPreference preference);
    }
}
