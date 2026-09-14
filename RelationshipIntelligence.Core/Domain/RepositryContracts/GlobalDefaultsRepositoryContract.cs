using Entities;
using System;
using System.Threading.Tasks;

namespace RepositryContracts
{
    public interface GlobalDefaultsRepositoryContract
    {
        Task<GlobalPreferenceDefaults?> GetAsync(Guid ownerId);

        Task UpsertAsync(GlobalPreferenceDefaults defaults);
    }
}
