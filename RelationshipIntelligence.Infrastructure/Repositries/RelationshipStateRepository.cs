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
    public class RelationshipStateRepository : RelationshipStateRepositoryContract
    {
        private readonly AppDBContext _db;
        private readonly ILogger<RelationshipStateRepository> _logger;

        public RelationshipStateRepository(AppDBContext db, ILogger<RelationshipStateRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task UpsertAsync(RelationshipState state)
        {
            using (Operation.Time("Upsert relationship state"))
            {
                if (state == null)
                    throw new ArgumentNullException(nameof(state));

                var existing = await _db.RelationshipStates
                    .FirstOrDefaultAsync(s => s.ApplicationUserId == state.ApplicationUserId
                        && s.PersonId == state.PersonId);

                if (existing == null)
                {
                    if (state.RelationshipStateId == Guid.Empty)
                        state.RelationshipStateId = Guid.NewGuid();
                    await _db.RelationshipStates.AddAsync(state);
                }
                else
                {
                    existing.TieStrength = state.TieStrength;
                    existing.LastContactAtUtc = state.LastContactAtUtc;
                    existing.CadenceReferenceDays = state.CadenceReferenceDays;
                    existing.SilenceQuantile = state.SilenceQuantile;
                    existing.IsBridge = state.IsBridge;
                    existing.UrgencyScore = state.UrgencyScore;
                    existing.UpdatedAtUtc = state.UpdatedAtUtc;
                }
            }
        }

        public async Task<List<RelationshipState>> ListForOwnerAsync(Guid ownerId)
        {
            if (ownerId == Guid.Empty)
                return new List<RelationshipState>();

            return await _db.RelationshipStates
                .Where(s => s.ApplicationUserId == ownerId)
                .ToListAsync();
        }

        public async Task<RelationshipState?> GetAsync(Guid ownerId, Guid personId)
        {
            if (ownerId == Guid.Empty)
                return null;

            return await _db.RelationshipStates
                .FirstOrDefaultAsync(s => s.ApplicationUserId == ownerId && s.PersonId == personId);
        }

        public async Task AddSnapshotAsync(RelationshipStateSnapshot snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            if (snapshot.RelationshipStateSnapshotId == Guid.Empty)
                snapshot.RelationshipStateSnapshotId = Guid.NewGuid();
            await _db.RelationshipStateSnapshots.AddAsync(snapshot);
        }

        public async Task<List<RelationshipStateSnapshot>> ListSnapshotsAsync(Guid ownerId, Guid personId)
        {
            if (ownerId == Guid.Empty)
                return new List<RelationshipStateSnapshot>();

            return await _db.RelationshipStateSnapshots
                .Where(s => s.ApplicationUserId == ownerId && s.PersonId == personId)
                .OrderBy(s => s.TakenAtUtc)
                .ToListAsync();
        }
    }
}
