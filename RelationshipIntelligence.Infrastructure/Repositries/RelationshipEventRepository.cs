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
    public class RelationshipEventRepository : RelationshipEventRepositoryContract
    {
        private readonly AppDBContext _db;
        private readonly ILogger<RelationshipEventRepository> _logger;

        public RelationshipEventRepository(AppDBContext db, ILogger<RelationshipEventRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<List<RelationshipEvent>> ListForPersonAsync(Guid ownerId, Guid personId)
        {
            using (Operation.Time("List relationship events for person"))
            {
                return await _db.RelationshipEvents
                    .Where(e => e.ApplicationUserId == ownerId && e.PersonId == personId)
                    .OrderBy(e => e.OccursOn)
                    .ToListAsync();
            }
        }

        public async Task<List<RelationshipEvent>> ListForOwnerAsync(Guid ownerId)
        {
            using (Operation.Time("List relationship events for owner"))
            {
                return await _db.RelationshipEvents
                    .Where(e => e.ApplicationUserId == ownerId)
                    .ToListAsync();
            }
        }

        public async Task<RelationshipEvent?> GetAsync(Guid ownerId, Guid eventId)
        {
            using (Operation.Time("Get relationship event"))
            {
                return await _db.RelationshipEvents
                    .FirstOrDefaultAsync(e => e.ApplicationUserId == ownerId && e.EventId == eventId);
            }
        }

        public async Task AddAsync(RelationshipEvent relationshipEvent)
        {
            if (relationshipEvent == null)
                throw new ArgumentNullException(nameof(relationshipEvent));

            if (relationshipEvent.EventId == Guid.Empty)
                relationshipEvent.EventId = Guid.NewGuid();

            await _db.RelationshipEvents.AddAsync(relationshipEvent);
        }

        public async Task RemoveAsync(RelationshipEvent relationshipEvent)
        {
            if (relationshipEvent == null)
                throw new ArgumentNullException(nameof(relationshipEvent));

            var tracked = await _db.RelationshipEvents
                .FirstOrDefaultAsync(e => e.ApplicationUserId == relationshipEvent.ApplicationUserId
                    && e.EventId == relationshipEvent.EventId);
            if (tracked != null)
                _db.RelationshipEvents.Remove(tracked);

            await Task.CompletedTask;
        }
    }
}
