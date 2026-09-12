using ServiceContracts.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceContracts
{
    public interface IRelationshipScoringService
    {
        Task RecomputeForPairAsync(Guid? personId);

        Task<int> RecomputePairsAsync(IEnumerable<Guid> personIds);

        Task<int> RecomputeForOwnerAsync(Guid ownerId);

        Task<int> RecomputeForCurrentUserAsync();

        Task<List<RelationshipHealthResponse>> GetQueueAsync(int top = 7);

        Task<RelationshipStateHistoryResponse> GetHistoryAsync(Guid? personId);
    }
}
