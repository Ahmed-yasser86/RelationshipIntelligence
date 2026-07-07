using ContactsManger.Core.DTOs.PersonDTOs;
using Entities;
using Microsoft.Extensions.Logging;
using RepositryContracts;
using SerilogTimings;
using ServiceContracts;
using ServiceContracts.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Servicess
{
    public class PersonSearcherService : IPersonSearcherService
    {
        private readonly PersonRepositryContract PersonRipository;
        private readonly ILogger<PersonSearcherService> _logger;

        public PersonSearcherService(PersonRepositryContract personRipository, ILogger<PersonSearcherService> logger)
        {
            PersonRipository = personRipository;
            _logger = logger;
        }
        // DON'T FORGET FUNCTION OF sort by interactions DES/ACEDN

        public async Task<PagedResult<PersonViewDTO>> SearchPersonsBy_Batched(string? PersonParamter, string SearchBy, int pageNumber, int pageSize)
        {
            using (Operation.Time("Search persons by {SearchBy} with parameter: {Parameter}", SearchBy ?? "none", PersonParamter ?? "null"))
            {
                _logger.LogInformation("Executing {MethodName} method at {Timestamp}. SearchBy: {SearchBy}, Parameter: {Parameter}",
                    nameof(SearchPersonsBy_Batched), DateTime.UtcNow, SearchBy, PersonParamter);

                try
                {
                    List<PersonViewDTO> MatchingResults = new List<PersonViewDTO>();
                    // total item count in database
                    int totalCount = 0;

                    switch (SearchBy)
                    {
                        case nameof(PersonRespones.Name):
                            {
                                _logger.LogDebug("Searching persons by Name containing: {Parameter}", PersonParamter);
                                var (people, count) = await PersonRipository.GetFilteredPersonsPaged(pageNumber, pageSize, p => p.Name != null && p.Name.Contains(PersonParamter));
                                totalCount = count;
                                MatchingResults = people
                                    .Where(p => p != null)
                                    .Select(p => p!.ConvertToPersonViewDTO())
                                    .ToList();
                                break;
                            }
                        case nameof(PersonRespones.email):
                            {
                                _logger.LogDebug("Searching persons by Email containing: {Parameter}", PersonParamter);
                                var (people, count) = await PersonRipository.GetFilteredPersonsPaged(pageNumber, pageSize, p => p.email != null && p.email.Contains(PersonParamter));
                                totalCount = count;
                                MatchingResults = people
                                    .Where(p => p != null)
                                    .Select(p => p!.ConvertToPersonViewDTO())
                                    .ToList();
                                break;
                            }
                        case nameof(PersonRespones.phone):
                            {
                                _logger.LogDebug("Searching persons by Phone containing: {Parameter}", PersonParamter);
                                var (people, count) = await PersonRipository.GetFilteredPersonsPaged(pageNumber, pageSize, p => p.phone != null && p.phone.Contains(PersonParamter));
                                totalCount = count;
                                MatchingResults = people
                                    .Where(p => p != null)
                                    .Select(p => p!.ConvertToPersonViewDTO())
                                    .ToList();
                                break;
                            }
                        case nameof(PersonRespones.DateOfBirth):
                            {
                                _logger.LogDebug("Searching persons by DateOfBirth containing: {Parameter}", PersonParamter);
                                var (people, count) = await PersonRipository.GetFilteredPersonsPaged(pageNumber, pageSize, p => p.DateOfBirth != null && p.DateOfBirth.Value.ToString("yyyy-MM-dd").Contains(PersonParamter));
                                totalCount = count;
                                MatchingResults = people
                                    .Where(p => p != null)
                                    .Select(p => p!.ConvertToPersonViewDTO())
                                    .ToList();
                                break;
                            }
                        case nameof(Person.Circles):
                            {
                                _logger.LogDebug("Searching persons by Circles containing: {Parameter}", PersonParamter);
                              
                                var (people, count) = await PersonRipository.GetFilteredPersonsPaged(pageNumber, pageSize, p => p.Circles.Any(c => c.Name.Contains(PersonParamter)));
                                totalCount = count;
                                MatchingResults = people
                                    .Where(p => p != null)
                                    .Select(p => p!.ConvertToPersonViewDTO())
                                    .ToList();
                                break;
                            }
                        case nameof(Person.ContactItemRoles):
                            {
                                _logger.LogDebug("Searching persons by Role containing: {Parameter}", PersonParamter);
                                var (people, count) = await PersonRipository.GetFilteredPersonsPaged(pageNumber, pageSize, p => p.ContactItemRoles.Any(c => c.Role.Contains(PersonParamter)));
                                totalCount = count;
                                MatchingResults = people
                                    .Where(p => p != null)
                                    .Select(p => p!.ConvertToPersonViewDTO())
                                    .ToList();
                                break;
                            }
                        case nameof(Person.SystemStatusTags):
                            {
                                _logger.LogDebug("Searching persons by SystemStatusTags containing: {Parameter}", PersonParamter);
                                var (people, count) = await PersonRipository.GetFilteredPersonsPaged(pageNumber, pageSize, p => p.SystemStatusTags.Any(c => c.Name.Contains(PersonParamter)));
                                totalCount = count;
                                MatchingResults = people
                                    .Where(p => p != null)
                                    .Select(p => p!.ConvertToPersonViewDTO())
                                    .ToList();
                                break;
                            }
                        case nameof(Person.UserDefinedTags):
                            {
                                _logger.LogDebug("Searching persons by UserDefinedTags containing: {Parameter}", PersonParamter);
                                var (people, count) = await PersonRipository.GetFilteredPersonsPaged(pageNumber, pageSize, p => p.UserDefinedTags.Any(c => c.TagName.Contains(PersonParamter)));
                                totalCount = count;
                                MatchingResults = people
                                    .Where(p => p != null)
                                    .Select(p => p!.ConvertToPersonViewDTO())
                                    .ToList();
                                break;
                            }
                        default:
                            {
                                _logger.LogWarning("Unknown search criteria: {SearchBy}. Returning all persons paginated.", SearchBy);
                                // Pass a 'true' predicate to get all records, but still respect pagination
                                var (people, count) = await PersonRipository.GetFilteredPersonsPaged(pageNumber, pageSize, p => true);
                                totalCount = count;
                                MatchingResults = people
                                    .Where(p => p != null)
                                    .Select(p => p!.ConvertToPersonViewDTO())
                                    .ToList();
                                break;
                            }
                    }

                    _logger.LogInformation("{MethodName} completed successfully. Found {Count} results for SearchBy: {SearchBy}, Parameter: {Parameter}",
                        nameof(SearchPersonsBy_Batched), MatchingResults.Count, SearchBy, PersonParamter);

                    return new PagedResult<PersonViewDTO>
                    {
                        Items = MatchingResults,
                        TotalCount = totalCount,
                        PageNumber = pageNumber,
                        PageSize = pageSize
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in {MethodName} method. SearchBy: {SearchBy}, Parameter: {Parameter}",
                        nameof(SearchPersonsBy_Batched), SearchBy, PersonParamter);
                    throw;
                }
            }
        }



        [Obsolete]
        public async Task<List<PersonRespones>> SearchPersonsBy(string? PersonParamter, string SearchBy)
        {
            using (Operation.Time("Search persons by {SearchBy} with parameter: {Parameter}", SearchBy ?? "none", PersonParamter ?? "null"))
            {
                _logger.LogInformation("Executing {MethodName} method at {Timestamp}. SearchBy: {SearchBy}, Parameter: {Parameter}",
                    nameof(SearchPersonsBy), DateTime.UtcNow, SearchBy, PersonParamter);

                try
                {
                    List<PersonRespones> MatchingResults = new List<PersonRespones>();

                    switch (SearchBy)
                    {
                        case nameof(PersonRespones.Name):
                            {
                                _logger.LogDebug("Searching persons by Name containing: {Parameter}", PersonParamter);
                                var filteredPersons = await PersonRipository.GetFilteredPersons(p => p.Name != null && p.Name.Contains(PersonParamter));
                                MatchingResults = filteredPersons
                                    .Where(p => p != null)
                                    .Select(p => p!.ConvertToPersonRespons())
                                    .ToList();
                                break;
                            }
                        case nameof(PersonRespones.email):
                            {
                                _logger.LogDebug("Searching persons by Email containing: {Parameter}", PersonParamter);
                                var filteredPersons = await PersonRipository.GetFilteredPersons(p => p.email != null && p.email.Contains(PersonParamter));
                                MatchingResults = filteredPersons
                                    .Where(p => p != null)
                                    .Select(p => p!.ConvertToPersonRespons())
                                    .ToList();
                                break;
                            }
                        case nameof(PersonRespones.phone):
                            {
                                _logger.LogDebug("Searching persons by Phone containing: {Parameter}", PersonParamter);
                                var filteredPersons = await PersonRipository.GetFilteredPersons(p => p.phone != null && p.phone.Contains(PersonParamter));
                                MatchingResults = filteredPersons
                                    .Where(p => p != null)
                                    .Select(p => p!.ConvertToPersonRespons())
                                    .ToList();
                                break;
                            }
                        case nameof(PersonRespones.DateOfBirth):
                            {
                                _logger.LogDebug("Searching persons by DateOfBirth containing: {Parameter}", PersonParamter);
                                var filteredPersons = await PersonRipository.GetFilteredPersons(p => p.DateOfBirth != null && p.DateOfBirth.Value.ToString("yyyy-MM-dd").Contains(PersonParamter));
                                MatchingResults = filteredPersons
                                    .Where(p => p != null)
                                    .Select(p => p!.ConvertToPersonRespons())
                                    .ToList();
                                break;
                            }
                        case nameof(Person.Circles):
                            {
                                _logger.LogDebug("Searching persons by Circles containing: {Parameter}", PersonParamter);
                                var filteredPersons = await PersonRipository.GetFilteredPersons(p => p.Circles.Any(c => c.Name.Contains(PersonParamter)));
                                MatchingResults = filteredPersons
                                    .Where(p => p != null)
                                    .Select(p => p!.ConvertToPersonRespons())
                                    .ToList();
                                break;
                            }
                        case nameof(Person.ContactItemRoles):
                            {
                                _logger.LogDebug("Searching persons by Role containing: {Parameter}", PersonParamter);
                                var filteredPersons = await PersonRipository.GetFilteredPersons(p => p.ContactItemRoles.Any(c => c.Role.Contains(PersonParamter)));
                                MatchingResults = filteredPersons
                                    .Where(p => p != null)
                                    .Select(p => p!.ConvertToPersonRespons())
                                    .ToList();
                                break;
                            }
                        case nameof(Person.SystemStatusTags):
                            {
                                _logger.LogDebug("Searching persons by SystemStatusTags containing: {Parameter}", PersonParamter);
                                var filteredPersons = await PersonRipository.GetFilteredPersons(p => p.SystemStatusTags.Any(c => c.Name.Contains(PersonParamter)));
                                MatchingResults = filteredPersons
                                    .Where(p => p != null)
                                    .Select(p => p!.ConvertToPersonRespons())
                                    .ToList();
                                break;
                            }
                        case nameof(Person.UserDefinedTags):
                            {
                                _logger.LogDebug("Searching persons by UserDefinedTags containing: {Parameter}", PersonParamter);
                                var filteredPersons = await PersonRipository.GetFilteredPersons(p => p.UserDefinedTags.Any(c => c.TagName.Contains(PersonParamter)));
                                MatchingResults = filteredPersons
                                    .Where(p => p != null)
                                    .Select(p => p!.ConvertToPersonRespons())
                                    .ToList();
                                break;
                            }
                        default:
                            {
                                _logger.LogWarning("Unknown search criteria: {SearchBy}. Returning all persons.", SearchBy);
                                var Persons = await PersonRipository.GetAllPersons();
                                MatchingResults = Persons
                                    .Select(p => p!.ConvertToPersonRespons())
                                    .ToList();
                                break;
                            }
                    }

                    _logger.LogInformation("{MethodName} completed successfully. Found {Count} results for SearchBy: {SearchBy}, Parameter: {Parameter}",
                        nameof(SearchPersonsBy), MatchingResults.Count, SearchBy, PersonParamter);

                    return MatchingResults;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in {MethodName} method. SearchBy: {SearchBy}, Parameter: {Parameter}",
                        nameof(SearchPersonsBy), SearchBy, PersonParamter);
                    throw;
                }
            }
        }
    }
}