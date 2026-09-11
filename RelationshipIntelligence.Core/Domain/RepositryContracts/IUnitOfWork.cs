
using System.Threading.Tasks;

namespace RepositryContracts
{
    /// <summary>
    /// Commits the changes tracked by the current <see cref="Entities.AppDBContext"/>
    /// scope. Consistency rule for this solution: repositories only attach and track
    /// entities (they never call SaveChanges themselves); each application-service
    /// use case performs exactly one <see cref="SaveChangesAsync"/> call through this
    /// contract when its unit of work is complete. A single SaveChanges call is
    /// atomic in Entity Framework Core, so no explicit transaction API is exposed
    /// until a cross-aggregate use case genuinely requires one.
    /// </summary>
    public interface IUnitOfWork
    {

        Task<int> SaveChangesAsync();
    }
}
