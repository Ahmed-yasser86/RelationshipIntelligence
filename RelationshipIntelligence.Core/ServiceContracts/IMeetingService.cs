using ServiceContracts.DTOs.MeetingDTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceContracts
{
    public interface IMeetingService
    {
        Task<List<MeetingResponse>> ListAsync();

        Task<List<MeetingResponse>> ListMeetingsForPersonAsync(Guid personId);

        Task<MeetingResponse> GetAsync(Guid meetingId);

        Task<MeetingResponse> CreatePrepAsync(MeetingCreateRequest request);

        Task<MeetingResponse> CreateDraftAsync(MeetingCreateRequest request);

        Task<MeetingResponse> BeginLoggingAsync(Guid meetingId, DateTime? actualOccurredAtUtc = null);

        Task<MeetingResponse> SetTranscriptAsync(MeetingTranscriptRequest request);

        Task<MeetingResponse> ProcessAsync(Guid meetingId);

        Task<MeetingResponse> UpdateMappingsAsync(Guid meetingId, List<PersonMappingRequest> mappings);

        Task<MeetingResponse> ReviewFindingAsync(FindingReviewRequest request);

        Task<MeetingResponse> SaveBriefAsync(BriefSaveRequest request);

        Task<MeetingResponse> GenerateBriefAsync(Guid meetingId);

        Task<MeetingResponse> ConfirmAsync(MeetingConfirmRequest request);

        Task DeleteAsync(Guid meetingId);
    }
}
