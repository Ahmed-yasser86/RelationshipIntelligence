using Entities;
using ServiceContracts.DTOs.MemoryDTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceContracts
{
    public interface IRelationshipMemoryService
    {
        Task<List<MemoryEntryResponse>> ListForPersonAsync(Guid personId);

        Task<MemoryEntryResponse> CreateAsync(MemoryEntryCreateRequest request);

        Task<MemoryEntryResponse> SuggestEntryAsync(Guid personId, RelationshipMemoryKind kind, string title, string? detail);

        Task<List<MemoryEntryResponse>> ProposeGapsAsync(Guid personId);

        Task<MemoryEntryResponse> UpdateAsync(MemoryEntryUpdateRequest request);

        Task DeleteAsync(Guid entryId);

        Task<MemoryEntryResponse> AcceptSuggestionAsync(Guid entryId, string? correctionNote = null);

        Task RejectSuggestionAsync(Guid entryId);
    }
}
