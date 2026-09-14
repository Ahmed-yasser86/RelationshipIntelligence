using Entities;
using ServiceContracts.DTOs.IngestionDTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceContracts
{
    public interface IIngestionService
    {
        Task<IngestionBatchDto> SubmitAsync(IngestionSubmitRequest request);

        /// <summary>
        /// Submits a meeting's actual content (transcript + notes) into the
        /// unified pipeline. Only ACTUAL EVIDENCE is used — agenda, preparation
        /// notes, and description never enter ingestion. Idempotent via hash.
        /// </summary>
        Task<IngestionBatchDto> SubmitForMeetingAsync(Guid meetingId);

        Task<IngestionBatchDto> ProcessAsync(Guid batchId);

        Task<IngestionBatchDto> GetAsync(Guid batchId);

        Task<List<IngestionBatchDto>> ListAsync();

        Task<List<IngestionFindingDto>> ListPendingAsync();

        Task<IngestionFindingDto> ReviewAsync(IngestionFindingReviewRequest request);

        Task<IngestionApplyResult> ApproveFindingAsync(Guid findingId);

        Task<IngestionApplyResult> ApprovePersonAsync(Guid batchId, Guid? personId, string? personName, bool isNew);

        Task<IngestionApplyResult> ApproveBatchAsync(Guid batchId);

        Task DiscardAsync(Guid batchId);
    }
}
