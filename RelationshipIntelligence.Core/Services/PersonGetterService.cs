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
    public class PersonGetterService : IPersonGetterService
    {
        private readonly PersonRepositryContract PersonRipository;
        private readonly ILogger<PersonGetterService> _logger;

        public PersonGetterService(PersonRepositryContract personRipository, ILogger<PersonGetterService> logger)
        {
            PersonRipository = personRipository;
            _logger = logger;
        }

        public async Task<PagedResult<PersonViewDTO>> GetPersonsViewBatched(int pageNumber, int pageSize)
        {
            using (Operation.Time("Get paged persons operation. Page: {PageNumber}, Size: {PageSize}", pageNumber, pageSize))
            {
                _logger.LogInformation("Executing {MethodName} method at {Timestamp}. PageNumber: {PageNumber}, PageSize: {PageSize}",
                    nameof(GetPersonsViewBatched), DateTime.UtcNow, pageNumber, pageSize);

                try
                {
                    if (pageNumber < 1)
                    {
                        _logger.LogWarning("GetPersonsViewBatched called with invalid PageNumber: {PageNumber}. Clamping to 1.", pageNumber);
                        pageNumber = 1;
                    }

                    if (pageSize < 1)
                    {
                        _logger.LogWarning("GetPersonsViewBatched called with invalid PageSize: {PageSize}. Clamping to 1.", pageSize);
                        pageSize = 1;
                    }

                    var (items, totalCount) = await PersonRipository.GetPersonsPaged(pageNumber, pageSize);

                    var result = new PagedResult<PersonViewDTO>
                    {
                        Items = items.Select(p => p.ConvertToPersonViewDTO()).ToList(),
                        TotalCount = totalCount,
                        PageNumber = pageNumber,
                        PageSize = pageSize
                    };

                    _logger.LogInformation("{MethodName} completed successfully. Retrieved {Count} of {TotalCount} persons (page {PageNumber})",
                        nameof(GetPersonsViewBatched), result.Items.Count, totalCount, pageNumber);

                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in {MethodName} method. PageNumber: {PageNumber}, PageSize: {PageSize}",
                        nameof(GetPersonsViewBatched), pageNumber, pageSize);
                    throw;
                }
            }
        }




        public async Task<List<PersonRespones>> GetAllPersons()
        {
            using (Operation.Time("Get all persons operation"))
            {
                _logger.LogInformation("Executing {MethodName} method at {Timestamp}",
                    nameof(GetAllPersons), DateTime.UtcNow);

                try
                {
                    var list = await PersonRipository.GetAllPersons();
                    var result = list.Select(p => p.ConvertToPersonRespons()).ToList();

                    _logger.LogInformation("{MethodName} completed successfully. Retrieved {Count} persons",
                        nameof(GetAllPersons), result.Count);

                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in {MethodName} method", nameof(GetAllPersons));
                    throw;
                }
            }
        }

        public async Task<PersonRespones?> GetPersonByPersonId(Guid? personId)
        {
            using (Operation.Time("Get person by ID operation for: {PersonId}", personId))
            {
                _logger.LogInformation("Executing {MethodName} method at {Timestamp}. PersonId: {PersonId}",
                    nameof(GetPersonByPersonId), DateTime.UtcNow, personId);

                try
                {
                    if (personId == Guid.Empty || personId == null)
                    {
                        _logger.LogWarning("GetPersonByPersonId called with invalid PersonId: {PersonId}", personId);
                        return null;
                    }

                    _logger.LogDebug("Retrieving person with ID: {PersonId}", personId);
                    Person? person = await PersonRipository.GetPersonById(personId);

                    if (person == null)
                    {
                        _logger.LogWarning("No person found with ID: {PersonId}", personId);
                        return null;
                    }

                    var result = person.ConvertToPersonRespons();
                    _logger.LogInformation("Successfully retrieved person with ID: {PersonId}, Name: {PersonName}",
                        result.PersonId, result.Name);

                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in GetPersonByPersonId for PersonId: {PersonId}", personId);
                    throw;
                }
            }
        }
    }
}