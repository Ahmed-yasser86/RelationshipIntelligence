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

        // reduced the latency from 30 seconds to 1/3 second by using AsSplitQuery() to avoid the cartesian product problem
        private IQueryable<Person> PersonWithAllIncludes()
        {
            return _db.Persons
                .Include(p => p.Country)
                .Include(p => p.Circles)
                .Include(p => p.ContactItemRoles)
                .Include(p => p.OtherSocialMediaAccounts)
                .Include(p => p.ConnectionChannels)
                .Include(p => p.SystemStatusTags)
                .Include(p => p.UserDefinedTags)
                .Include(p => p.Notes)
                .Include(p => p.Interactions)
                .AsSplitQuery();
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


        public async Task<Person> AddPerson(Person person)
        {
            await _db.Persons.AddAsync(person);
            await _db.SaveChangesAsync();
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
            await _db.SaveChangesAsync();
            return true;
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

        /// <summary>
        /// Persists an already-fully-resolved Person (scalar fields + navigation
        /// collections containing real tracked-or-new entities). This method does
        /// NOT perform any get-or-create / lookup logic for Circles, ContactItemRoles,
        /// tags, etc. — that resolution is the Service layer's responsibility, per
        /// Clean Architecture: the Repository only persists what it's given.
        /// </summary>
        public async Task<Person> UpdatePerson(Person person)
        {
            var existingPerson = await PersonWithAllIncludes()
                .FirstOrDefaultAsync(p => p.PersonId == person.PersonId);

            if (existingPerson == null)
                throw new InvalidOperationException($"Person with ID {person.PersonId} does not exist.");

            // CurrentValues.SetValues only copies scalar/primitive properties;
            // it intentionally does not touch navigation collections.
            _db.Entry(existingPerson).CurrentValues.SetValues(person);

            // Navigation collections must be synced explicitly. We replace the
            // tracked collection's contents with whatever the incoming Person
            // carries. EF Core's change tracker diffs this against the DB state
            // on SaveChanges (additions/removals of join rows for the M:N sets,
            // and FK reassignment for the 1:N ContactItemRoles).
            SyncCollection(existingPerson.Circles, person.Circles);
            SyncCollection(existingPerson.ContactItemRoles, person.ContactItemRoles);
            SyncCollection(existingPerson.OtherSocialMediaAccounts, person.OtherSocialMediaAccounts);
            SyncCollection(existingPerson.ConnectionChannels, person.ConnectionChannels);
            SyncCollection(existingPerson.SystemStatusTags, person.SystemStatusTags);
            SyncCollection(existingPerson.UserDefinedTags, person.UserDefinedTags);
            SyncCollection(existingPerson.Notes, person.Notes);
            SyncCollection(existingPerson.Interactions, person.Interactions);

            await _db.SaveChangesAsync();
            return existingPerson;
        }

        /// <summary>
        /// Replaces the contents of a tracked navigation collection with the
        /// incoming set, without swapping out the collection instance itself
        /// (EF Core needs the tracked ICollection reference to stay stable).
        /// </summary>
        private static void SyncCollection<T>(ICollection<T> tracked, ICollection<T>? incoming)
        {
            if (incoming == null)
                return;

            tracked.Clear();
            foreach (var item in incoming)
            {
                tracked.Add(item);
            }
        }

      
    }
}