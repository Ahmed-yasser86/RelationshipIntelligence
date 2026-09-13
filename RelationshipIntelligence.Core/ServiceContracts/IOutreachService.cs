using Entities;
using ServiceContracts.DTOs.CopilotDTOs;
using ServiceContracts.DTOs.OutreachDTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceContracts
{
    public interface IOutreachService
    {
        Task<OutreachBatchResponse> BuildFromSignalsAsync(BuildBatchFromSignalsRequest request);

        Task<OutreachBatchResponse> BuildFromPersonsAsync(BuildBatchFromPersonsRequest request);

        Task<OutreachBatchResponse> BuildFromIntentAsync(BatchIntentDto intent);

        Task<List<OutreachBatchResponse>> ListAsync();

        Task<OutreachBatchResponse> GetAsync(Guid batchId);

        Task<OutreachBatchResponse> UpdateSettingsAsync(Guid batchId, BatchSettingsRequest request);

        Task<OutreachBatchResponse> UpdateMemberAsync(Guid batchId, Guid memberId, MemberOverrideRequest request);

        Task<OutreachBatchResponse> GenerateDraftsAsync(Guid batchId);

        Task<DraftCommunicationResult> PreviewDraftAsync(Guid personId, OutreachChannel channel, string intent, string? instruction);

        Task<CommunicationDraftDto> ReviewDraftAsync(Guid draftId, DraftReviewRequest request);

        Task<CommunicationDraftDto> RegenerateDraftAsync(Guid draftId, string? customInstruction);
        Task<OutreachBatchResponse> ApproveAsync(Guid batchId, List<Guid>? draftIds);

        Task DiscardBatchAsync(Guid batchId);
    }
}
