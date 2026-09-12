using Entities;
using System;
using System.Threading.Tasks;

namespace RepositryContracts
{
    public interface AiProviderSettingsRepositoryContract
    {
        Task<AiProviderSettings?> GetAsync(Guid ownerId);

        Task UpsertAsync(AiProviderSettings settings);
    }
}
