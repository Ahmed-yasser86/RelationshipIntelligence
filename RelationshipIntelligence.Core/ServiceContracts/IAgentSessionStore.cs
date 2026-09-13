using ServiceContracts.DTOs.AgentDTOs;
using System;
using System.Threading.Tasks;

namespace ServiceContracts
{
    public interface IAgentSessionStore
    {
        Task<AgentSession> GetOrCreateAsync(Guid? sessionId, Guid ownerId);

        Task SaveAsync(Guid ownerId, AgentSession session);
    }
}
