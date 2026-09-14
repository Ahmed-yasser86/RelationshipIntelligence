using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RepositryContracts;
using SerilogTimings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Repositories
{
    public class RelationshipPreferenceRepository : RelationshipPreferenceRepositoryContract
    {
        private readonly AppDBContext _db;
        private readonly ILogger<RelationshipPreferenceRepository> _logger;

        public RelationshipPreferenceRepository(AppDBContext db, ILogger<RelationshipPreferenceRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<RelationshipPreference?> GetAsync(Guid ownerId, Guid personId)
        {
            using (Operation.Time("Get relationship preference"))
            {
                return await _db.RelationshipPreferences
                    .FirstOrDefaultAsync(p => p.ApplicationUserId == ownerId && p.PersonId == personId);
            }
        }

        public async Task<List<RelationshipPreference>> ListForOwnerAsync(Guid ownerId)
        {
            using (Operation.Time("List relationship preferences"))
            {
                return await _db.RelationshipPreferences
                    .Where(p => p.ApplicationUserId == ownerId)
                    .ToListAsync();
            }
        }

        public async Task AddAsync(RelationshipPreference preference)
        {
            if (preference == null)
                throw new ArgumentNullException(nameof(preference));

            if (preference.RelationshipPreferenceId == Guid.Empty)
                preference.RelationshipPreferenceId = Guid.NewGuid();

            await _db.RelationshipPreferences.AddAsync(preference);
        }

        public async Task RemoveAsync(RelationshipPreference preference)
        {
            if (preference == null)
                throw new ArgumentNullException(nameof(preference));

            var tracked = await _db.RelationshipPreferences
                .FirstOrDefaultAsync(p => p.ApplicationUserId == preference.ApplicationUserId
                    && p.RelationshipPreferenceId == preference.RelationshipPreferenceId);
            if (tracked != null)
                _db.RelationshipPreferences.Remove(tracked);

            await Task.CompletedTask;
        }
    }
}
