using ServiceContracts.DTOs.CopilotDTOs;
using System.Threading.Tasks;

namespace ServiceContracts
{
    public interface IAiProviderSettingsService
    {
        Task<AiProviderSettingsResponse> GetAsync();

        Task<AiProviderSettingsResponse> SaveAsync(AiProviderSettingsSaveRequest request);

        Task<string?> GetUnprotectedKeyAsync();
    }
}
