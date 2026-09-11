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
using Microsoft.EntityFrameworkCore;

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
        private readonly SocialMediaAccountRepositryContract _socialMediaAccountRepositryContract;
        private readonly IUnitOfWork _unitOfWork;

        public PersonUpdaterService(
            PersonRepositryContract personRipository,
            CircleRepositryContract circleRepository,
            ContactItemRoleRepositryContract contactItemRoleRepository,
            ConnectionChannelRepositryContract connectionChannelRepository,
            UserDefinedTagsRepositryContract userDefinedTagsRepository,
            SystemStatusTagRepositryContract systemStatusTagRepository,
            ILogger<PersonUpdaterService> logger,
            SocialMediaAccountRepositryContract socialMediaAccountRepositryContract,
            IUnitOfWork unitOfWork)
        {
            PersonRipository = personRipository;
            _circleRepository = circleRepository;
            _contactItemRoleRepository = contactItemRoleRepository;
            _connectionChannelRepository = connectionChannelRepository;
            _userDefinedTagsRepository = userDefinedTagsRepository;
            _systemStatusTagRepository = systemStatusTagRepository;
            _logger = logger;
            _socialMediaAccountRepositryContract = socialMediaAccountRepositryContract;
            _unitOfWork = unitOfWork;
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
                    person.Gender = personUpdateRequest.Gender?.ToString() ?? person.Gender;

                    person.ContextMemory = personUpdateRequest.ContextMemory ?? person.ContextMemory;
                    person.ProfileImagePath = personUpdateRequest.ProfileImagePath ?? person.ProfileImagePath;
                    person.Origin = personUpdateRequest.Origin ?? person.Origin;
                    person.LinkedInProfile = personUpdateRequest.LinkedInProfile ?? person.LinkedInProfile;
                    person.OtherInformation = personUpdateRequest.OtherInformation ?? person.OtherInformation;



                    if (personUpdateRequest.Organizations != null)
                    {
                        await SyncCircles(person, personUpdateRequest.Organizations);
                    }

                    if (personUpdateRequest.CurrentRoles != null)
                    {
                        await SyncContactItemRoles(person, personUpdateRequest.CurrentRoles);
                    }

                    if (personUpdateRequest.ConnectionChannels != null)
                    {
                        await SyncConnectionChannels(person, personUpdateRequest.ConnectionChannels);
                    }

                    if (personUpdateRequest.UserDefinedTags != null)
                    {
                        await SyncUserDefinedTags(person, personUpdateRequest.UserDefinedTags);
                    }

                    if (personUpdateRequest.SystemStatusTags != null)
                    {
                        await SyncSystemStatusTags(person, personUpdateRequest.SystemStatusTags);
                    }

                    if (personUpdateRequest.SocialMediaAccounts != null)
                    {
                        await SyncSocialMediaAccounts(person, personUpdateRequest.SocialMediaAccounts);
                    }

                    await _unitOfWork.SaveChangesAsync();

                    var result = person.ConvertToPersonRespons();

                    _logger.LogInformation("Successfully updated person with ID: {PersonId}, Name: {PersonName}",
                        result.PersonId, result.Name);

                    return result;
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    foreach (var entry in ex.Entries)
                    {
                        Console.WriteLine($"Entity: {entry.Entity.GetType().Name}");
                        Console.WriteLine($"State : {entry.State}");

                        foreach (var property in entry.Properties)
                        {
                            Console.WriteLine($"{property.Metadata.Name} = {property.CurrentValue}");
                        }

                        Console.WriteLine("------------------------");
                    }

                    throw;
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


        private async Task SyncCircles(Person person, List<string> organizationNames)
        {
            var requestedNames = organizationNames
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var currentCircles = person.Circles.ToList();

            var untouched = currentCircles
                .Where(c => requestedNames.Any(n => string.Equals(n, c.Name, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var toRemove = currentCircles.Except(untouched).ToList();
            foreach (var circle in toRemove)
                person.Circles.Remove(circle);

            var untouchedNames = untouched
                .Select(c => c.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var namesStillNeeded = requestedNames
                .Where(n => !untouchedNames.Contains(n))
                .ToList();

            if (namesStillNeeded.Count == 0) return;

            var foundCircles = (await _circleRepository.GetCirclesByNames(namesStillNeeded)).ToList();
            var foundNames = foundCircles
                .Select(c => c.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var circle in foundCircles)
                person.Circles.Add(circle);

            foreach (var name in namesStillNeeded.Where(n => !foundNames.Contains(n)))
            {
                person.Circles.Add(new Circle { CircleId = Guid.NewGuid(), Name = name });
            }
        }

        private async Task SyncContactItemRoles(Person person, List<string> roleNames)
        {
            var requestedRoles = roleNames
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var currentRoles = person.ContactItemRoles.ToList();

            var untouchedRoles = currentRoles
                .Where(existing => requestedRoles.Any(r => string.Equals(r, existing.Role, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var rolesStillNeeded = requestedRoles
                .Where(r => !untouchedRoles.Any(existing => string.Equals(existing.Role, r, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var rowsAvailableForReuse = currentRoles
                .Except(untouchedRoles)
                .ToList();

            int pairCount = Math.Min(rowsAvailableForReuse.Count, rolesStillNeeded.Count);

            for (int i = 0; i < pairCount; i++)
            {
                rowsAvailableForReuse[i].Role = rolesStillNeeded[i];
            }

            for (int i = pairCount; i < rowsAvailableForReuse.Count; i++)
            {
                person.ContactItemRoles.Remove(rowsAvailableForReuse[i]);
            }

            for (int i = pairCount; i < rolesStillNeeded.Count; i++)
            {
                var roleName = rolesStillNeeded[i];

                var existingElsewhere = await _contactItemRoleRepository.GetContactItemRoleByPersonAndRole(person.PersonId, roleName);

                person.ContactItemRoles.Add(existingElsewhere ?? new ContactItemRole
                {
                    ContactsRoleId = Guid.NewGuid(),
                    Role = roleName,
                    PersonId = person.PersonId
                });
            }
        }


        private async Task SyncConnectionChannels(Person person, List<string> channelNames)
        {
            var requestedNames = channelNames
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var currentChannels = person.ConnectionChannels.ToList();

            var untouched = currentChannels
                .Where(c => requestedNames.Any(n => string.Equals(n, c.ConnectionChannelName, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var toRemove = currentChannels.Except(untouched).ToList();
            foreach (var channel in toRemove)
                person.ConnectionChannels.Remove(channel);

            var untouchedNames = untouched
                .Select(c => c.ConnectionChannelName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var namesStillNeeded = requestedNames
                .Where(n => !untouchedNames.Contains(n))
                .ToList();

            foreach (var name in namesStillNeeded)
            {
                var existing = await _connectionChannelRepository.GetConnectionChannelByName(name);
                person.ConnectionChannels.Add(existing ?? new ConnectionChannel
                {
                    ConnectionChannelId = Guid.NewGuid(),
                    ConnectionChannelName = name
                });
            }
        }


        private async Task SyncUserDefinedTags(Person person, List<string> tagNames)
        {
            var requestedNames = tagNames
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var currentTags = person.UserDefinedTags.ToList();

            var untouched = currentTags
                .Where(t => requestedNames.Any(n => string.Equals(n, t.TagName, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var toRemove = currentTags.Except(untouched).ToList();
            foreach (var tag in toRemove)
                person.UserDefinedTags.Remove(tag);

            var untouchedNames = untouched
                .Select(t => t.TagName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var namesStillNeeded = requestedNames
                .Where(n => !untouchedNames.Contains(n))
                .ToList();

            foreach (var name in namesStillNeeded)
            {
                var existing = await _userDefinedTagsRepository.GetUserDefinedTagByName(name);
                person.UserDefinedTags.Add(existing ?? new UserDefinedTags
                {
                    TagId = Guid.NewGuid(),
                    TagName = name
                });
            }
        }


        private async Task SyncSystemStatusTags(
            Person person,
            List<ContactsManger.Core.Domain.Entities.EEnums.EnSystemStatusTag> statusTags)
        {
            var requested = statusTags.Distinct().ToList();

            var current = person.SystemStatusTags.ToList();

            var untouched = current
                .Where(t => requested.Contains(t.StatusTagId))
                .ToList();

            var toRemove = current.Except(untouched).ToList();
            foreach (var tag in toRemove)
                person.SystemStatusTags.Remove(tag);

            var untouchedEnums = untouched.Select(t => t.StatusTagId).ToHashSet();
            var stillNeeded = requested.Where(e => !untouchedEnums.Contains(e)).ToList();

            foreach (var statusEnum in stillNeeded)
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

        private Task SyncSocialMediaAccounts(Person person, List<SocialMediaAccountAddRequest> accounts)
        {
            var requested = accounts.Where(a => !string.IsNullOrWhiteSpace(a.Url)).ToList();
            var current = person.OtherSocialMediaAccounts.ToList();

            int pairCount = Math.Min(requested.Count, current.Count);

            for (int i = 0; i < pairCount; i++)
            {
                current[i].Platform = requested[i].Platform;
                current[i].Url = requested[i].Url;
            }

            for (int i = pairCount; i < current.Count; i++)
            {
                person.OtherSocialMediaAccounts.Remove(current[i]);
            }

            for (int i = pairCount; i < requested.Count; i++)
            {
                person.OtherSocialMediaAccounts.Add(new SocialMediaAccount
                {
                    SocialMediaAccountId = Guid.NewGuid(),
                    Platform = requested[i].Platform,
                    Url = requested[i].Url
                });
            }

            return Task.CompletedTask;
        }
    }
}