using Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RepositryContracts
{
    public interface IngestionRepositoryContract
    {
        Task<List<IngestionBatch>> ListAsync(Guid ownerId);

        Task<IngestionBatch?> GetAsync(Guid ownerId, Guid batchId);

        Task<IngestionBatch?> FindByHashAsync(Guid ownerId, IngestionSourceType sourceType, Guid? sourceMeetingId, string hash);

        Task AddBatchAsync(IngestionBatch batch);

        Task<List<IngestionFinding>> ListFindingsAsync(Guid ownerId, Guid batchId);

        Task<IngestionFinding?> GetFindingAsync(Guid ownerId, Guid findingId);

        Task<List<IngestionFinding>> ListPendingAsync(Guid ownerId);

        Task AddFindingAsync(IngestionFinding finding);
    }
}
