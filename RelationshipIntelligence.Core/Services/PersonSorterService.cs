using ContactsManger.Core.Domain.Entities.EEnums;
using ContactsManger.Core.DTOs.PersonDTOs;
using Microsoft.Extensions.Logging;
using RepositryContracts;
using SerilogTimings;
using ServiceContracts;
using ServiceContracts.DTOs;
using ServiceContracts.DTOs.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Servicess
{
    public class PersonSorterService : IPersonSorterService
    {
        private readonly PersonRepositryContract PersonRipository;
        private readonly ILogger<PersonSorterService> _logger;


        public PersonSorterService(PersonRepositryContract personRipository, ILogger<PersonSorterService> logger)
        {
            PersonRipository = personRipository;
            _logger = logger;
        }

        /// <summary>
        /// A person's "last interaction" is the MAX TimeOfInteraction across
        /// their Interactions collection -- you can't sort by a List directly,
        /// you have to derive a single comparable value from it first. Persons
        /// with zero interactions get DateTime.MinValue, so they consistently
        /// sort to the bottom of a "most recent first" ordering rather than
        /// unpredictably interleaving with people who do have interaction
        /// history.
        /// </summary>
        private static DateTime GetLastInteractionDate(PersonViewDTO person)
        {
            return person.Interactions.Count == 0
                ? DateTime.MinValue
                : person.Interactions.Max(i => i.TimeOfInteraction);
        }

        /// <summary>
        /// A person's "most important tag" is the MIN EnSystemStatusTag value
        /// across their SystemStatusTags collection, since the enum is already
        /// defined in priority order (HighPriority = 1 ... Ignored = 10 -- see
        /// EnSystemStatusTag). Lower number = more urgent. Persons with zero
        /// tags get int.MaxValue so they consistently sort to the bottom
        /// regardless of Ascending/Descending direction (never confused for a
        /// genuinely low-priority tag).
        /// </summary>
        private static int GetMostImportantTagRank(PersonViewDTO person)
        {
            return person.SystemStatusTags.Count == 0
                ? int.MaxValue
                : person.SystemStatusTags.Min(t => (int)t.StatusTagId);
        }

        /// <summary>
        /// This Function Is Used With MVC projects only 
        /// </summary>
        /// <param name="persons"></param>
        /// <param name="sortBy"></param>
        /// <param name="sortOrder"></param>
        /// <returns></returns>
        public async Task<List<PersonViewDTO>> getPersonsSorted(List<PersonViewDTO> persons, string? sortBy, sortedListOp sortOrder)
        {
            using (Operation.Time("Sort {Count} persons by {SortBy} ({SortOrder})", persons?.Count ?? 0, sortBy ?? "none", sortOrder))
            {
                _logger.LogInformation("Executing {MethodName} method at {Timestamp}. SortBy: {SortBy}, SortOrder: {SortOrder}, PersonsCount: {PersonsCount}",
                    nameof(getPersonsSorted), DateTime.UtcNow, sortBy, sortOrder, persons?.Count ?? 0);

                try
                {
                    if (persons == null)
                    {
                        _logger.LogWarning("getPersonsSorted called with null persons list");
                        return new List<PersonViewDTO>();
                    }

                    if (string.IsNullOrEmpty(sortBy))
                    {
                        _logger.LogDebug("No sort criteria provided, returning unsorted list");
                        return persons;
                    }

                    List<PersonViewDTO> sortedPersons = (sortOrder, sortBy) switch
                    {
                        (sortedListOp.Ascending, nameof(PersonViewDTO.Name)) => persons.OrderBy(p => p.Name).ToList(),
                        (sortedListOp.Ascending, nameof(PersonViewDTO.email)) => persons.OrderBy(p => p.email).ToList(),
                        (sortedListOp.Ascending, nameof(PersonViewDTO.phone)) => persons.OrderBy(p => p.phone).ToList(),
                        (sortedListOp.Ascending, nameof(PersonViewDTO.CountryName)) => persons.OrderBy(p => p.CountryName).ToList(),
                        (sortedListOp.Ascending, nameof(PersonViewDTO.PersonId)) => persons.OrderBy(p => p.PersonId).ToList(),
                        (sortedListOp.Ascending, nameof(PersonViewDTO.Interactions)) =>
                            persons.OrderBy(GetLastInteractionDate).ToList(),
                        (sortedListOp.Ascending, nameof(PersonViewDTO.SystemStatusTags)) =>
                            persons.OrderBy(GetMostImportantTagRank).ToList(),

                        (sortedListOp.Descending, nameof(PersonViewDTO.Name)) => persons.OrderByDescending(p => p.Name).ToList(),
                        (sortedListOp.Descending, nameof(PersonViewDTO.email)) => persons.OrderByDescending(p => p.email).ToList(),
                        (sortedListOp.Descending, nameof(PersonViewDTO.phone)) => persons.OrderByDescending(p => p.phone).ToList(),
                        (sortedListOp.Descending, nameof(PersonViewDTO.CountryName)) => persons.OrderByDescending(p => p.CountryName).ToList(),
                        (sortedListOp.Descending, nameof(PersonViewDTO.PersonId)) => persons.OrderByDescending(p => p.PersonId).ToList(),

                        (sortedListOp.Descending, nameof(PersonViewDTO.Interactions)) =>
                            persons.OrderByDescending(GetLastInteractionDate).ToList(),
                        (sortedListOp.Descending, nameof(PersonViewDTO.SystemStatusTags)) =>
                            persons.OrderByDescending(GetMostImportantTagRank).ToList(),

                        _ => persons
                    };

                    _logger.LogInformation("{MethodName} completed successfully. Sorted {Count} persons by {SortBy} ({SortOrder})",
                        nameof(getPersonsSorted), sortedPersons.Count, sortBy, sortOrder);

                    return sortedPersons;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in {MethodName} method. SortBy: {SortBy}, SortOrder: {SortOrder}",
                        nameof(getPersonsSorted), sortBy, sortOrder);
                    throw;
                }
            }
        }



    }
}