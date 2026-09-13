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
    public class RelationshipMemoryRepository : RelationshipMemoryRepositoryContract
    {
        private readonly AppDBContext _db;
        private readonly ILogger<RelationshipMemoryRepository> _logger;

        public RelationshipMemoryRepository(AppDBContext db, ILogger<RelationshipMemoryRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<List<RelationshipMemoryEntry>> ListForPersonAsync(Guid ownerId, Guid personId)
        {
            using (Operation.Time("List relationship memory entries"))
            {
                return await _db.RelationshipMemoryEntries
                    .Where(e => e.ApplicationUserId == ownerId && e.PersonId == personId)
                    .OrderBy(e => e.Kind)
                    .ThenBy(e => e.CreatedAtUtc)
                    .ToListAsync();
            }
        }

        public async Task<RelationshipMemoryEntry?> GetAsync(Guid ownerId, Guid entryId)
        {
            using (Operation.Time("Get relationship memory entry"))
            {
                return await _db.RelationshipMemoryEntries
                    .FirstOrDefaultAsync(e => e.ApplicationUserId == ownerId && e.MemoryEntryId == entryId);
            }
        }

        public async Task<List<RelationshipMemoryEntry>> ListByMeetingAsync(Guid ownerId, Guid meetingId)
        {
            using (Operation.Time("List memory entries by meeting"))
            {
                return await _db.RelationshipMemoryEntries
                    .Where(e => e.ApplicationUserId == ownerId && e.SourceMeetingId == meetingId)
                    .ToListAsync();
            }
        }

        public async Task AddAsync(RelationshipMemoryEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            if (entry.MemoryEntryId == Guid.Empty)
                entry.MemoryEntryId = Guid.NewGuid();

            await _db.RelationshipMemoryEntries.AddAsync(entry);
        }

        public async Task RemoveAsync(RelationshipMemoryEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            var tracked = await _db.RelationshipMemoryEntries
                .FirstOrDefaultAsync(e => e.ApplicationUserId == entry.ApplicationUserId
                    && e.MemoryEntryId == entry.MemoryEntryId);
            if (tracked != null)
                _db.RelationshipMemoryEntries.Remove(tracked);

            await Task.CompletedTask;
        }
    }
}
