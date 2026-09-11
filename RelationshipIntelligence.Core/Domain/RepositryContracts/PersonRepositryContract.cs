using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Entities;
using ContactsManger.Core.Domain.Entities.EEnums.SortDirection;
namespace RepositryContracts
{
    public interface PersonRepositryContract
    {

        Task<Person> AddPerson(Person person);

        [Obsolete("Update flows mutate the tracked entity loaded via GetPersonById and commit once through IUnitOfWork. This merge API is retained only for compatibility and is no longer called by any service.")]
        Task<Person> UpdatePerson(Person person);

        Task<Person>? GetPersonById(Guid? id);

        /// <summary>
        /// Bypasses the global ownership query filter. Reserved exclusively for flows
        /// where an HMAC-signed token has already established authorization
        /// (digest one-click actions). The caller MUST verify ownership explicitly.
        /// </summary>
        Task<Person?> GetPersonByIdIgnoringFilters(Guid? id);

        Task<List<Person>> ListByIdsAsync(IEnumerable<Guid> personIds);

        Task<List<PersonAffinity>> ListAffinitiesAsync();

        Task<IEnumerable<Person>> GetAllPersons();

        Task<List<Person?>> GetFilteredPersons(Expression<Func<Person, bool>> predicate);

        Task<(List<Person> Items, int TotalCount)> GetFilteredPersonsPaged(int pageNumber, int pageSize, Expression<Func<Person, bool>> predicate);

        public Task<(List<Person> Items, int TotalCount)> GetSortedFilteredPersonsPaged(
             int pageNumber,
             int pageSize,
             Expression<Func<Person, bool>> predicate,
             string sortBy,
             SortDirection sortDirection = SortDirection.Ascending);
        Task<bool> DeletePerson(Guid? id);

        Task<(List<Person> Items, int TotalCount)> GetPersonsPaged(int pageNumber, int pageSize);

    }
}
