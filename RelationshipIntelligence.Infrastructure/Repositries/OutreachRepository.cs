using Entities;
using Microsoft.EntityFrameworkCore;
using RepositryContracts;
using SerilogTimings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Repositories
{
    public class OutreachRepository : OutreachRepositoryContract
    {
        private readonly AppDBContext _db;

        public OutreachRepository(AppDBContext db)
        {
            _db = db;
        }

        private IQueryable<OutreachBatch> WithDetails() => _db.OutreachBatches
            .Include(b => b.Members)
            .Include(b => b.Drafts);

        public async Task<List<OutreachBatch>> ListBatchesAsync(Guid ownerId)
        {
            using (Operation.Time("List outreach batches"))
            {
                if (ownerId == Guid.Empty)
                    return new List<OutreachBatch>();

                return await _db.OutreachBatches
                    .Where(b => b.ApplicationUserId == ownerId)
                    .OrderByDescending(b => b.CreatedAtUtc)
                    .ToListAsync();
            }
        }

        public async Task<OutreachBatch?> GetBatchAsync(Guid ownerId, Guid batchId)
        {
            using (Operation.Time("Get outreach batch"))
            {
                if (ownerId == Guid.Empty)
                    return null;

                return await WithDetails()
                    .FirstOrDefaultAsync(b => b.ApplicationUserId == ownerId && b.OutreachBatchId == batchId);
            }
        }

        public async Task AddBatchAsync(OutreachBatch batch)
        {
            if (batch == null)
                throw new ArgumentNullException(nameof(batch));

            if (batch.OutreachBatchId == Guid.Empty)
                batch.OutreachBatchId = Guid.NewGuid();

            await _db.OutreachBatches.AddAsync(batch);
        }

        public async Task AddDraftAsync(CommunicationDraft draft)
        {
            if (draft == null)
                throw new ArgumentNullException(nameof(draft));

            if (draft.CommunicationDraftId == Guid.Empty)
                draft.CommunicationDraftId = Guid.NewGuid();

            await _db.CommunicationDrafts.AddAsync(draft);
        }

        public async Task<List<OutreachBatch>> ListRecentBatchesAsync(Guid ownerId, DateTime since)
        {
            if (ownerId == Guid.Empty)
                return new List<OutreachBatch>();

            return await WithDetails()
                .Where(b => b.ApplicationUserId == ownerId && b.CreatedAtUtc >= since)
                .ToListAsync();
        }
    }
}
