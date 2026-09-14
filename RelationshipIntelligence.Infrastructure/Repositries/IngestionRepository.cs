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
    public class IngestionRepository : IngestionRepositoryContract
    {
        private readonly AppDBContext _db;
        private readonly ILogger<IngestionRepository> _logger;

        public IngestionRepository(AppDBContext db, ILogger<IngestionRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<List<IngestionBatch>> ListAsync(Guid ownerId)
        {
            using (Operation.Time("List ingestion batches"))
            {
                return await _db.IngestionBatches
                    .Where(b => b.ApplicationUserId == ownerId)
                    .OrderByDescending(b => b.CreatedAtUtc)
                    .ToListAsync();
            }
        }

        public async Task<IngestionBatch?> GetAsync(Guid ownerId, Guid batchId)
        {
            using (Operation.Time("Get ingestion batch"))
            {
                return await _db.IngestionBatches
                    .Include(b => b.Findings)
                    .FirstOrDefaultAsync(b => b.ApplicationUserId == ownerId && b.IngestionBatchId == batchId);
            }
        }

        public async Task<IngestionBatch?> FindByHashAsync(Guid ownerId, IngestionSourceType sourceType, Guid? sourceMeetingId, string hash)
        {
            using (Operation.Time("Find ingestion batch by hash"))
            {
                return await _db.IngestionBatches
                    .Include(b => b.Findings)
                    .FirstOrDefaultAsync(b => b.ApplicationUserId == ownerId
                        && b.SourceType == sourceType
                        && b.SourceMeetingId == sourceMeetingId
                        && b.SourceTextHash == hash);
            }
        }

        public async Task AddBatchAsync(IngestionBatch batch)
        {
            if (batch == null)
                throw new ArgumentNullException(nameof(batch));

            if (batch.IngestionBatchId == Guid.Empty)
                batch.IngestionBatchId = Guid.NewGuid();

            await _db.IngestionBatches.AddAsync(batch);
        }

        public async Task<List<IngestionFinding>> ListFindingsAsync(Guid ownerId, Guid batchId)
        {
            using (Operation.Time("List ingestion findings"))
            {
                return await _db.IngestionFindings
                    .Where(f => f.ApplicationUserId == ownerId && f.IngestionBatchId == batchId)
                    .OrderBy(f => f.SubjectName)
                    .ThenBy(f => f.CreatedAtUtc)
                    .ToListAsync();
            }
        }

        public async Task<IngestionFinding?> GetFindingAsync(Guid ownerId, Guid findingId)
        {
            using (Operation.Time("Get ingestion finding"))
            {
                return await _db.IngestionFindings
                    .FirstOrDefaultAsync(f => f.ApplicationUserId == ownerId && f.IngestionFindingId == findingId);
            }
        }

        public async Task<List<IngestionFinding>> ListPendingAsync(Guid ownerId)
        {
            using (Operation.Time("List pending ingestion findings"))
            {
                return await _db.IngestionFindings
                    .Where(f => f.ApplicationUserId == ownerId
                        && (f.Status == IngestionFindingStatus.Pending || f.Status == IngestionFindingStatus.Unresolved))
                    .OrderByDescending(f => f.CreatedAtUtc)
                    .Take(100)
                    .ToListAsync();
            }
        }

        public async Task AddFindingAsync(IngestionFinding finding)
        {
            if (finding == null)
                throw new ArgumentNullException(nameof(finding));

            if (finding.IngestionFindingId == Guid.Empty)
                finding.IngestionFindingId = Guid.NewGuid();

            await _db.IngestionFindings.AddAsync(finding);
        }
    }
}
