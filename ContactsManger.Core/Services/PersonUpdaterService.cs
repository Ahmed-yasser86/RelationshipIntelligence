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
    public class PersonUpdaterService : IPersonUpdaterService
    {
        private readonly PersonRepositryContract PersonRipository;
        private readonly CircleRepositryContract _circleRepository;
        private readonly ContactItemRoleRepositryContract _contactItemRoleRepository;
        private readonly ConnectionChannelRepositryContract _connectionChannelRepository;
        private readonly UserDefinedTagsRepositryContract _userDefinedTagsRepository;
        private readonly SystemStatusTagRepositryContract _systemStatusTagRepository;
        private readonly ILogger<PersonUpdaterService> _logger;

        public PersonUpdaterService(
            PersonRepositryContract personRipository,
            CircleRepositryContract circleRepository,
            ContactItemRoleRepositryContract contactItemRoleRepository,
            ConnectionChannelRepositryContract connectionChannelRepository,
            UserDefinedTagsRepositryContract userDefinedTagsRepository,
            SystemStatusTagRepositryContract systemStatusTagRepository,
            ILogger<PersonUpdaterService> logger)
        {
            PersonRipository = personRipository;
            _circleRepository = circleRepository;
            _contactItemRoleRepository = contactItemRoleRepository;
            _connectionChannelRepository = connectionChannelRepository;
            _userDefinedTagsRepository = userDefinedTagsRepository;
            _systemStatusTagRepository = systemStatusTagRepository;
            _logger = logger;
        }

        public async Task<PersonRespones?> UpdatePerson(PersonUpdateRequest? personUpdateRequest)
        {
            using (Operation.Time("Update person operation for ID: {PersonId}", personUpdateRequest?.PersonId))
            {
                _logger.LogInformation("Executing {MethodName} method at {Timestamp}. Request data: {@PersonUpdateRequest}",
                    nameof(UpdatePerson), DateTime.UtcNow, personUpdateRequest);

                try
                {
                    if (personUpdateRequest == null)
                    {
                        _logger.LogWarning("UpdatePerson called with null request parameter");
                        throw new ArgumentNullException(nameof(personUpdateRequest));
                    }

                    _logger.LogDebug("Validating person update request");
                    ValidationHelpers.ValidationFunction(personUpdateRequest);

                    _logger.LogDebug("Retrieving existing person with ID: {PersonId}", personUpdateRequest.PersonId);
                    var person = await PersonRipository?.GetPersonById(personUpdateRequest.PersonId);

                    if (person == null)
                    {
                        _logger.LogWarning("Attempted to update non-existent person with ID: {PersonId}", personUpdateRequest.PersonId);
                        throw new ArgumentException("Given person ID does not exist.");
                    }

                    _logger.LogDebug("Updating person fields for ID: {PersonId}", personUpdateRequest.PersonId);

                    // --- Scalar fields (unchanged behavior from the original service) ---
                    if (personUpdateRequest.Name != null && person.Name != personUpdateRequest.Name)
                        _logger.LogDebug("Updating Name from '{OldValue}' to '{NewValue}'", person.Name, personUpdateRequest.Name);

                    if (personUpdateRequest.DateOfBirth != null && person.DateOfBirth != personUpdateRequest.DateOfBirth)
                        _logger.LogDebug("Updating DateOfBirth from '{OldValue}' to '{NewValue}'", person.DateOfBirth, personUpdateRequest.DateOfBirth);

                    if (personUpdateRequest.email != null && person.email != personUpdateRequest.email)
                        _logger.LogDebug("Updating Email from '{OldValue}' to '{NewValue}'", person.email, personUpdateRequest.email);

                    if (personUpdateRequest.phone != null && person.phone != personUpdateRequest.phone)
                        _logger.LogDebug("Updating Phone from '{OldValue}' to '{NewValue}'", person.phone, personUpdateRequest.phone);

                    if (personUpdateRequest.Address != null && person.Address != personUpdateRequest.Address)
                        _logger.LogDebug("Updating Address from '{OldValue}' to '{NewValue}'", person.Address, personUpdateRequest.Address);

                    person.Name = personUpdateRequest.Name ?? person.Name;
                    person.DateOfBirth = personUpdateRequest.DateOfBirth ?? person.DateOfBirth;
                    person.email = personUpdateRequest.email ?? person.email;
                    person.phone = personUpdateRequest.phone ?? person.phone;
                    person.NewsLetter = personUpdateRequest.NewsLetter ?? person.NewsLetter;
                    person.Address = personUpdateRequest.Address ?? person.Address;
                    person.CountryId = personUpdateRequest.CountryId ?? person.CountryId;
                    person.Gender = personUpdateRequest.Gender.ToString() ?? person.Gender;

                    // --- New full-profile scalar fields ---
                    person.ContextMemory = personUpdateRequest.ContextMemory ?? person.ContextMemory;
                    person.ProfileImagePath = personUpdateRequest.ProfileImagePath ?? person.ProfileImagePath;
                    person.Origin = personUpdateRequest.Origin ?? person.Origin;
                    person.LinkedInProfile = personUpdateRequest.LinkedInProfile ?? person.LinkedInProfile;
                    person.OtherInformation = personUpdateRequest.OtherInformation ?? person.OtherInformation;

                    // --- Related collections: resolve get-or-create here (Service
                    // layer), then hand the fully-formed Person to the Repository,
                    // which only syncs what it's given (no lookups in Repository). ---
                    if (personUpdateRequest.Organizations != null)
                    {
                        person.Circles.Clear();
                        await ResolveCircles(person, personUpdateRequest.Organizations);
                    }

                    if (personUpdateRequest.CurrentRoles != null)
                    {
                        person.ContactItemRoles.Clear();
                        ResolveContactItemRoles(person, personUpdateRequest.CurrentRoles);
                    }

                    if (personUpdateRequest.ConnectionChannels != null)
                    {
                        person.ConnectionChannels.Clear();
                        await ResolveConnectionChannels(person, personUpdateRequest.ConnectionChannels);
                    }

                    if (personUpdateRequest.UserDefinedTags != null)
                    {
                        person.UserDefinedTags.Clear();
                        await ResolveUserDefinedTags(person, personUpdateRequest.UserDefinedTags);
                    }

                    if (personUpdateRequest.SystemStatusTags != null)
                    {
                        person.SystemStatusTags.Clear();
                        await ResolveSystemStatusTags(person, personUpdateRequest.SystemStatusTags);
                    }

                    if (personUpdateRequest.SocialMediaAccounts != null)
                    {
                        person.OtherSocialMediaAccounts.Clear();
                        ResolveSocialMediaAccounts(person, personUpdateRequest.SocialMediaAccounts);
                    }

                    await PersonRipository.UpdatePerson(person);

                    var result = person.ConvertToPersonRespons();

                    _logger.LogInformation("Successfully updated person with ID: {PersonId}, Name: {PersonName}",
                        result.PersonId, result.Name);

                    return result;
                }
                catch (ArgumentException ex)
                {
                    _logger.LogWarning(ex, "Argument error in UpdatePerson for PersonId: {PersonId}",
                        personUpdateRequest?.PersonId);
                    throw;
                }
                catch (ValidationException ex)
                {
                    _logger.LogWarning(ex, "Validation error in UpdatePerson for request: {@PersonUpdateRequest}", personUpdateRequest);
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error occurred in UpdatePerson for request: {@PersonUpdateRequest}", personUpdateRequest);
                    throw;
                }
            }
        }

        private async Task ResolveCircles(Person person, List<string> organizationNames)
        {
            foreach (var name in organizationNames.Where(n => !string.IsNullOrWhiteSpace(n)))
            {
                var existing = await _circleRepository.GetCircleByName(name);
                person.Circles.Add(existing ?? new Circle { CircleId = Guid.NewGuid(), Name = name });
            }
        }

        private void ResolveContactItemRoles(Person person, List<string> roleNames)
        {
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

        private async Task ResolveConnectionChannels(Person person, List<string> channelNames)
        {
            foreach (var name in channelNames.Where(n => !string.IsNullOrWhiteSpace(n)))
            {
                var existing = await _connectionChannelRepository.GetConnectionChannelByName(name);
                person.ConnectionChannels.Add(existing ?? new ConnectionChannel
                {
                    ConnectionChannelId = Guid.NewGuid(),
                    ConnectionChannelName = name
                });
            }
        }

        private async Task ResolveUserDefinedTags(Person person, List<string> tagNames)
        {
            foreach (var name in tagNames.Where(n => !string.IsNullOrWhiteSpace(n)))
            {
                var existing = await _userDefinedTagsRepository.GetUserDefinedTagByName(name);
                person.UserDefinedTags.Add(existing ?? new UserDefinedTags
                {
                    TagId = Guid.NewGuid(),
                    TagName = name
                });
            }
        }

        private async Task ResolveSystemStatusTags(Person person, List<ContactsManger.Core.Domain.Entities.EEnums.EnSystemStatusTag> statusTags)
        {
            foreach (var statusEnum in statusTags)
            {
                var existing = await _systemStatusTagRepository.GetSystemStatusTagByEnum(statusEnum);
                if (existing != null)
                {
                    person.SystemStatusTags.Add(existing);
                }
                else
                {
                    _logger.LogWarning("SystemStatusTag reference row missing for enum {StatusTagId}; skipping.", statusEnum);
                }
            }
        }

        private void ResolveSocialMediaAccounts(Person person, List<SocialMediaAccountAddRequest> accounts)
        {
            foreach (var account in accounts.Where(a => !string.IsNullOrWhiteSpace(a.Url)))
            {
                person.OtherSocialMediaAccounts.Add(new SocialMediaAccount
                {
                    SocialMediaAccountId = Guid.NewGuid(),
                    Platform = account.Platform,
                    Url = account.Url
                });
            }
        }
    }
}