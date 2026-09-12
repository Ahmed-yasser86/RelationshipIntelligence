using ServiceContracts.DTOs.CopilotDTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceContracts
{
    public interface ICopilotService
    {
        Task<CopilotAnswer> AskAsync(string question, Guid? personId, List<ChatTurnDto>? history);

        Task<CopilotAnswer> SummarizePersonAsync(Guid personId);

        Task<BriefingDto> BuildBriefingAsync();

        Task<PlanSuggestionDto> SuggestPlanAsync(Guid personId, Guid? intentEntryId);

        Task<BatchIntentDto> ParseOutreachIntentAsync(string text);
    }
}
