using ServiceContracts.DTOs.AgentDTOs;
using System.Threading.Tasks;

namespace ServiceContracts
{
    public interface ICopilotAgent
    {
        Task<AgentResponse> ChatAsync(AgentChatRequest request);
    }
}
