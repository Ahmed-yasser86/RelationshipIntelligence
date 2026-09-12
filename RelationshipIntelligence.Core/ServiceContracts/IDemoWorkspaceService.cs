using System.Threading.Tasks;

namespace ServiceContracts
{
    public interface IDemoWorkspaceService
    {
        Task<int> SeedAsync();

        Task<int> ClearAsync();
    }
}
