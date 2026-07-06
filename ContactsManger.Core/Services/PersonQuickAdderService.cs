using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using ContactsManger.Core.Domain.Entities;
using Entities;
using Microsoft.Extensions.Logging;
using ServiceContracts;
using ServiceContracts.DTOs;
using Servicess.Helpers;
using RepositryContracts;
using SerilogTimings;

namespace Servicess
{
    public class PersonQuickAdderService : IPersonQuickAdderService
    {
        private readonly PersonRepositryContract PersonRipository;
        private readonly CircleRepositryContract _circleRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<PersonQuickAdderService> _logger;

        public PersonQuickAdderService(
            PersonRepositryContract personRipository,
            CircleRepositryContract circleRepository,
            ICurrentUserService currentUserService,
            ILogger<PersonQuickAdderService> logger)
        {
            PersonRipository = personRipository;
            _circleRepository = circleRepository;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<PersonRespones> QuickAddPerson(PersonQuickAddRequest? personQuickAddRequest)
        {
            using (Operation.Time("Quick add person operation for: {PersonName}", personQuickAddRequest?.Name ?? "null"))
            {
                _logger.LogInformation("Executing {MethodName} method at {Timestamp}. Request data: {@PersonQuickAddRequest}",
                    nameof(QuickAddPerson), DateTime.UtcNow, personQuickAddRequest);

                try
                {
                    if (personQuickAddRequest == null)
                    {
                        _logger.LogWarning("QuickAddPerson called with null request parameter");
                        throw new ArgumentNullException(nameof(personQuickAddRequest));
                    }

                    _logger.LogDebug("Validating quick add request");
                    ValidationHelpers.ValidationFunction(personQuickAddRequest);

                    _logger.LogDebug("Converting PersonQuickAddRequest to Person entity");
                    var person = personQuickAddRequest.ToPerson();
                    person.PersonId = Guid.NewGuid();

                    if (_currentUserService.UserId == null)
                    {
                        _logger.LogWarning("QuickAddPerson called with no authenticated user context");
                        throw new UnauthorizedAccessException("Cannot add a person without an authenticated user.");
                    }

                    person.ApplicationUserId = _currentUserService.UserId.Value;

                    await ResolveCircles(person, personQuickAddRequest.Organizations);
                    ResolveContactItemRoles(person, personQuickAddRequest.CurrentRoles);

                    _logger.LogDebug("Quick-adding new person with ID: {PersonId}, Name: {PersonName}",
                        person.PersonId, person.Name);

                    await PersonRipository.AddPerson(person);

                    var result = person.ConvertToPersonRespons();
                    result.CountryName = person.Country?.CountryName;

                    _logger.LogInformation("Successfully quick-added new person. ID: {PersonId}, Name: {PersonName}",
                        result.PersonId, result.Name);

                    return result;
                }
                catch (ValidationException ex)
                {
                    _logger.LogWarning(ex, "Validation error in QuickAddPerson for request: {@PersonQuickAddRequest}", personQuickAddRequest);
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error occurred in QuickAddPerson for request: {@PersonQuickAddRequest}", personQuickAddRequest);
                    throw;
                }
            }
        }

        private async Task ResolveCircles(Person person, List<string>? organizationNames)
        {
            if (organizationNames == null) return;

            foreach (var name in organizationNames.Where(n => !string.IsNullOrWhiteSpace(n)))
            {
                var existing = await _circleRepository.GetCircleByName(name);
                person.Circles.Add(existing ?? new Circle { CircleId = Guid.NewGuid(), Name = name });
            }
        }

        private void ResolveContactItemRoles(Person person, List<string>? roleNames)
        {
            if (roleNames == null) return;

            foreach (var role in roleNames.Where(r => !string.IsNullOrWhiteSpace(r)))
            {
                person.ContactItemRoles.Add(new ContactItemRole
                {
                    ContactsRoleId = Guid.NewGuid(),
                    Role = role,
                    PersonId = person.PersonId
                });
            }
        }
    }
}