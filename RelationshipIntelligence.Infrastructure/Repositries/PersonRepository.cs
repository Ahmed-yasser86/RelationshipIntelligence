using ContactsManger.Core.Domain.Entities.EEnums;
using ContactsManger.Core.Domain.Entities.EEnums.SortDirection;
using Entities;
using Microsoft.EntityFrameworkCore;
using RepositryContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Repositories
{
    public class PersonRepository : PersonRepositryContract
    {
        private readonly AppDBContext _db;

        public PersonRepository(AppDBContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Applies every Include() needed so PersonRespones.ConvertToPersonRespons()
        /// never sees an unintentionally empty collection. Centralized here so all
        /// read paths (GetAll, GetById, GetFiltered) stay consistent.
        /// </summary>
        /// 

        private IQueryable<Person> PersonWithAllIncludes()
        {
            return _db.Persons
                .Include(p => p.Country)
                .Include(p => p.Circles)
                .Include(p => p.ContactItemRoles)
                .Include(p => p.OtherSocialMediaAccounts)
                .Include(p => p.ContactChannels)
                    .ThenInclude(cc => cc.Channel)
                .Include(p => p.SystemStatusTags)
                .Include(p => p.UserDefinedTags)
                .Include(p => p.Notes)
                .Include(p => p.Interactions)
                .AsSplitQuery();
        }




        public async Task<Person> AddPerson(Person person)
        {

            _db.Persons.Add(person);
            return person;
        }

        public async Task<bool> DeletePerson(Guid? id)
        {
            if (id == null)
                return false;

            var person = await _db.Persons.FindAsync(id);
            if (person == null || person.IsDeleted)
                return false;

            person.IsDeleted = true;
            return true;
        }


        public async Task<Person> UpdatePerson(Person person)
        {
            var existingPerson = await PersonWithAllIncludes()
                .FirstOrDefaultAsync(p => p.PersonId == person.PersonId);

            if (existingPerson == null)
                throw new InvalidOperationException($"Person with ID {person.PersonId} does not exist.");

            _db.Entry(existingPerson).CurrentValues.SetValues(person);

            await SyncCollection(existingPerson.Circles, person.Circles, n => n.CircleId);
            await SyncCollection(existingPerson.ContactItemRoles, person.ContactItemRoles, n => n.ContactsRoleId);
            await SyncCollection(existingPerson.OtherSocialMediaAccounts, person.OtherSocialMediaAccounts, n => n.SocialMediaAccountId);
            await SyncCollection(existingPerson.ContactChannels, person.ContactChannels, n => n.ConnectionChannelId);
            await SyncCollection(existingPerson.SystemStatusTags, person.SystemStatusTags, n => n.StatusTagId);
            await SyncCollection(existingPerson.UserDefinedTags, person.UserDefinedTags, n => n.TagId);
            await SyncCollection(existingPerson.Notes, person.Notes, n => n.NoteId);
            await SyncCollection(existingPerson.Interactions, person.Interactions, n => n.InteractionId);

            return existingPerson;
        }



        public async Task<(List<Person> Items, int TotalCount)> GetFilteredPersonsPaged(
            int pageNumber, int pageSize, Expression<Func<Person, bool>> predicate)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 1;

            var baseQuery = PersonWithAllIncludes().AsNoTracking().Where(predicate);

            int totalCount = await baseQuery.CountAsync();

            var items = await baseQuery
                .OrderBy(p => p.Name)
                .ThenBy(p => p.PersonId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(List<Person> Items, int TotalCount)> GetSortedFilteredPersonsPaged(
       int pageNumber,
       int pageSize,
       Expression<Func<Person, bool>> predicate,
       string sortBy,
       SortDirection sortDirection = SortDirection.Ascending)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 1;

            var baseQuery = PersonWithAllIncludes().AsNoTracking()
                            .Where(p => !p.IsDeleted)
                            .Where(predicate);

            int totalCount = await baseQuery.CountAsync();

            IOrderedQueryable<Person> orderedQuery;

            if (sortDirection == SortDirection.Ascending)
            {
                orderedQuery = sortBy switch
                {
                    nameof(Person.Interactions) => baseQuery.OrderBy(p => p.Interactions.Max(i => (DateTime?)i.TimeOfInteraction) ?? DateTime.MinValue),
                    nameof(Person.SystemStatusTags) => baseQuery.OrderBy(p => p.SystemStatusTags.Min(t => (int?)t.StatusTagId) ?? int.MaxValue),
                    _ => baseQuery.OrderBy(p => p.Name)
                };
            }
            else
            {
                orderedQuery = sortBy switch
                {
                    nameof(Person.Interactions) => baseQuery.OrderByDescending(p => p.Interactions.Max(i => (DateTime?)i.TimeOfInteraction) ?? DateTime.MinValue),
                    nameof(Person.SystemStatusTags) => baseQuery.OrderByDescending(p => p.SystemStatusTags.Min(t => (int?)t.StatusTagId) ?? int.MaxValue),
                    _ => baseQuery.OrderByDescending(p => p.Name)
                };
            }

            var items = await orderedQuery
                .ThenBy(p => p.PersonId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }



        [Obsolete]
        public async Task<List<Person>> GetFilteredPersons(Expression<Func<Person, bool>> predicate)
        {
            return await PersonWithAllIncludes().AsNoTracking().Where(predicate).ToListAsync();
        }


        public async Task<(List<Person> Items, int TotalCount)> GetPersonsPaged(int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 1;


            var baseQuery = PersonWithAllIncludes().AsNoTracking();

            int totalCount = await _db.Persons.CountAsync();

            var items = await baseQuery
               .OrderBy(p => p.Name)
               .ThenBy(p => p.PersonId)
               .Skip((pageNumber - 1) * pageSize)
               .Take(pageSize)
               .ToListAsync();

            return (items, totalCount);
        }


        [Obsolete]
        public async Task<IEnumerable<Person>> GetAllPersons()
        {
            return await PersonWithAllIncludes().AsNoTracking().ToListAsync();
        }





        public async Task<Person?> GetPersonById(Guid? id)
        {
            if (id == null)
                return null;


            return await PersonWithAllIncludes()
                .FirstOrDefaultAsync(p => p.PersonId == id);
        }

        public async Task<Person?> GetPersonByIdIgnoringFilters(Guid? id)
        {
            if (id == null)
                return null;

            return await PersonWithAllIncludes()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.PersonId == id);
        }

        public async Task<List<Person>> ListByIdsAsync(IEnumerable<Guid> personIds)
        {
            var ids = personIds?.ToHashSet() ?? new HashSet<Guid>();
            if (ids.Count == 0)
                return new List<Person>();

            return await PersonWithAllIncludes()
                .Where(p => ids.Contains(p.PersonId))
                .ToListAsync();
        }

        public async Task<List<PersonAffinity>> ListAffinitiesAsync()
        {
            return await _db.Persons
                .Select(p => new PersonAffinity(
                    p.PersonId,
                    p.Name,
                    p.Circles.Select(c => c.Name).ToList(),
                    p.UserDefinedTags.Select(t => t.TagName).ToList(),
                    p.ContactChannels.Select(c => c.Channel.ConnectionChannelName).ToList(),
                    p.SystemStatusTags.Select(t => t.Name).ToList(),
                    p.Interactions.Max(i => (DateTime?)i.TimeOfInteraction)))
                .ToListAsync();
        }



        private async Task SyncCollection<TEntity, TKey>(
     ICollection<TEntity> tracked,
     ICollection<TEntity>? incoming,
     Func<TEntity, TKey> keySelector) where TEntity : class
        {
            if (incoming == null)
                return;

            var incomingKeys = incoming.Select(keySelector).ToHashSet();

            var itemsToRemove = tracked.Where(t => !incomingKeys.Contains(keySelector(t))).ToList();
            foreach (var item in itemsToRemove)
            {
                tracked.Remove(item);
            }

            foreach (var incomingItem in incoming)
            {
                var key = keySelector(incomingItem);
                var existingItem = tracked.FirstOrDefault(t => key.Equals(keySelector(t)));

                if (existingItem == null)
                {
                    tracked.Add(incomingItem);
                }
                else
                {
                    _db.Entry(existingItem).CurrentValues.SetValues(incomingItem);
                }
            }
        }


    }
}