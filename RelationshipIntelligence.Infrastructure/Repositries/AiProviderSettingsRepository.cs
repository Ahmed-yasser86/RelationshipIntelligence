using Entities;
using Microsoft.EntityFrameworkCore;
using RepositryContracts;
using System;
using System.Threading.Tasks;

namespace Repositories
{
    public class AiProviderSettingsRepository : AiProviderSettingsRepositoryContract
    {
        private readonly AppDBContext _db;

        public AiProviderSettingsRepository(AppDBContext db)
        {
            _db = db;
        }

        public async Task<AiProviderSettings?> GetAsync(Guid ownerId)
        {
            if (ownerId == Guid.Empty)
                return null;

            return await _db.AiProviderSettings
                .FirstOrDefaultAsync(s => s.ApplicationUserId == ownerId);
        }

        public async Task UpsertAsync(AiProviderSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            var existing = await _db.AiProviderSettings
                .FirstOrDefaultAsync(s => s.ApplicationUserId == settings.ApplicationUserId);
            if (existing == null)
            {
                await _db.AiProviderSettings.AddAsync(settings);
                return;
            }

            existing.Provider = settings.Provider;
            existing.Model = settings.Model;
            existing.BaseUrl = settings.BaseUrl;
            existing.ProtectedApiKey = settings.ProtectedApiKey;
            existing.UpdatedAtUtc = settings.UpdatedAtUtc;
        }
    }
}
