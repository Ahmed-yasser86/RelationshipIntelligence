
using System.Threading.Tasks;

namespace RepositryContracts
{
    public interface IUnitOfWork
    {
        
        Task<int> SaveChangesAsync();
    }
}
