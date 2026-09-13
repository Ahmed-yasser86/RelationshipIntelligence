using Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RepositryContracts
{
    public interface OutreachRepositoryContract
    {
        Task<List<OutreachBatch>> ListBatchesAsync(Guid ownerId);

        Task<OutreachBatch?> GetBatchAsync(Guid ownerId, Guid batchId);

        Task AddBatchAsync(OutreachBatch batch);

        Task AddDraftAsync(CommunicationDraft draft);

        Task<List<OutreachBatch>> ListRecentBatchesAsync(Guid ownerId, DateTime since);
    }
}
