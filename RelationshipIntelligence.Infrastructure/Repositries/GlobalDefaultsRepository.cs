using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RepositryContracts;
using SerilogTimings;
using System;
using System.Threading.Tasks;

namespace Repositories
{
    public class GlobalDefaultsRepository : GlobalDefaultsRepositoryContract
    {
        private readonly AppDBContext _db;
        private readonly ILogger<GlobalDefaultsRepository> _logger;

        public GlobalDefaultsRepository(AppDBContext db, ILogger<GlobalDefaultsRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<GlobalPreferenceDefaults?> GetAsync(Guid ownerId)
        {
            using (Operation.Time("Get global preference defaults"))
            {
                return await _db.GlobalPreferenceDefaults
                    .FirstOrDefaultAsync(d => d.ApplicationUserId == ownerId);
            }
        }

        public async Task UpsertAsync(GlobalPreferenceDefaults defaults)
        {
            if (defaults == null)
                throw new ArgumentNullException(nameof(defaults));

            var existing = await _db.GlobalPreferenceDefaults
                .FirstOrDefaultAsync(d => d.ApplicationUserId == defaults.ApplicationUserId);
            if (existing == null)
            {
                await _db.GlobalPreferenceDefaults.AddAsync(defaults);
                return;
            }

            existing.DefaultCadenceDays = defaults.DefaultCadenceDays;
            existing.DefaultReminderStrict = defaults.DefaultReminderStrict;
            existing.UpdatedAtUtc = defaults.UpdatedAtUtc;
        }
    }
}
