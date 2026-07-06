using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ServiceContracts;
using ServiceContracts.DTOs;
using ServiceContracts.DTOs.Enums;
using RepositryContracts;
using SerilogTimings;

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

        public async Task<List<PersonRespones>> getPersonsSorted(List<PersonRespones> persons, string? sortBy, sortedListOp sortOrder)
        {
            using (Operation.Time("Sort {Count} persons by {SortBy} ({SortOrder})", persons?.Count ?? 0, sortBy ?? "none", sortOrder))
            {
                _logger.LogInformation("Executing {MethodName} method at {Timestamp}. SortBy: {SortBy}, SortOrder: {SortOrder}, PersonsCount: {PersonsCount}",
                    nameof(getPersonsSorted), DateTime.UtcNow, sortBy, sortOrder, persons?.Count ?? 0);

                try
                {
                    if (string.IsNullOrEmpty(sortBy))
                    {
                        _logger.LogDebug("No sort criteria provided, returning unsorted list");
                        return persons;
                    }

                    List<PersonRespones> sortedPersons = (sortOrder, sortOrder) switch
                    {
                        (sortedListOp.Ascending, _) => sortBy switch
                        {
                            nameof(PersonRespones.Name) => persons.OrderBy(p => p.Name).ToList(),
                            nameof(PersonRespones.email) => persons.OrderBy(p => p.email).ToList(),
                            nameof(PersonRespones.phone) => persons.OrderBy(p => p.phone).ToList(),
                            nameof(PersonRespones.DateOfBirth) => persons.OrderBy(p => p.DateOfBirth).ToList(),
                            nameof(PersonRespones.CountryName) => persons.OrderBy(p => p.CountryName).ToList(),
                            nameof(PersonRespones.Age) => persons.OrderBy(p => p.Age).ToList(),
                            nameof(PersonRespones.PersonId) => persons.OrderBy(p => p.PersonId).ToList(),
                            nameof(PersonRespones.Address) => persons.OrderBy(p => p.Address).ToList(),
                            nameof(PersonRespones.Organizations) => persons.OrderBy(p => p.Organizations).ToList(),

                            _ => persons
                        },
                        (sortedListOp.Descending, _) => sortBy switch
                        {
                            nameof(PersonRespones.Name) => persons.OrderByDescending(p => p.Name).ToList(),
                            nameof(PersonRespones.email) => persons.OrderByDescending(p => p.email).ToList(),
                            nameof(PersonRespones.phone) => persons.OrderByDescending(p => p.phone).ToList(),
                            nameof(PersonRespones.DateOfBirth) => persons.OrderByDescending(p => p.DateOfBirth).ToList(),
                            nameof(PersonRespones.CountryName) => persons.OrderByDescending(p => p.CountryName).ToList(),
                            nameof(PersonRespones.Age) => persons.OrderByDescending(p => p.Age).ToList(),
                            nameof(PersonRespones.PersonId) => persons.OrderByDescending(p => p.PersonId).ToList(),
                            nameof(PersonRespones.Address) => persons.OrderByDescending(p => p.Address).ToList(),
                            nameof(PersonRespones.Organizations) => persons.OrderByDescending(p => p.Organizations).ToList(),

                            _ => persons
                        },
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