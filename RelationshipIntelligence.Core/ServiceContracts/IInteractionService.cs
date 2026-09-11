using ServiceContracts.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceContracts
{
    public interface IInteractionService
    {
        Task<InteractionResponse> LogAsync(InteractionAddRequest? request);

        Task<List<InteractionResponse>> ListForPersonAsync(Guid? personId);

        Task<int> ImportCsvAsync(Guid? personId, string? csvText);
    }
}
