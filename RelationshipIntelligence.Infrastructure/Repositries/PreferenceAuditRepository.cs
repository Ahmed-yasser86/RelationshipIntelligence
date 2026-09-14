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
    public class PreferenceAuditRepository : PreferenceAuditRepositoryContract
    {
        private readonly AppDBContext _db;
        private readonly ILogger<PreferenceAuditRepository> _logger;

        public PreferenceAuditRepository(AppDBContext db, ILogger<PreferenceAuditRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task AddAsync(PreferenceAuditEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            if (entry.PreferenceAuditEntryId == Guid.Empty)
                entry.PreferenceAuditEntryId = Guid.NewGuid();

            await _db.PreferenceAuditEntries.AddAsync(entry);
        }

        public async Task<List<PreferenceAuditEntry>> ListForPersonAsync(Guid ownerId, Guid personId)
        {
            using (Operation.Time("List preference audit entries"))
            {
                return await _db.PreferenceAuditEntries
                    .Where(a => a.ApplicationUserId == ownerId && a.PersonId == personId)
                    .OrderByDescending(a => a.ChangedAtUtc)
                    .Take(100)
                    .ToListAsync();
            }
        }
    }
}
