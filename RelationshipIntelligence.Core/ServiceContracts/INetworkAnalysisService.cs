using ServiceContracts.DTOs;
using System.Threading.Tasks;

namespace ServiceContracts
{
    public interface INetworkAnalysisService
    {
        Task<NetworkGraphResponse> GetGraphAsync();
    }
}
