using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using ContactsManger.Core.Domain.Entities;
using ContactsManger.Core.Domain.Entities.EEnums;
using Entities;
using Microsoft.Extensions.Logging;
using ServiceContracts;
using ServiceContracts.DTOs;
using Servicess.Helpers;
using RepositryContracts;
using SerilogTimings;

namespace Servicess
{
    public class PersonAdderService : IPersonAdderService
    {
        private readonly PersonRepositryContract PersonRipository;
        private readonly CircleRepositryContract _circleRepository;
        private readonly ContactItemRoleRepositryContract _contactItemRoleRepository;
        private readonly ConnectionChannelRepositryContract _connectionChannelRepository;
        private readonly UserDefinedTagsRepositryContract _userDefinedTagsRepository;
        private readonly SystemStatusTagRepositryContract _systemStatusTagRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PersonAdderService> _logger;

        public PersonAdderService(
            PersonRepositryContract personRipository,
            CircleRepositryContract circleRepository,
            ContactItemRoleRepositryContract contactItemRoleRepository,
            ConnectionChannelRepositryContract connectionChannelRepository,
            UserDefinedTagsRepositryContract userDefinedTagsRepository,
            SystemStatusTagRepositryContract systemStatusTagRepository,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork,
            ILogger<PersonAdderService> logger)
        {
            PersonRipository = personRipository;
            _circleRepository = circleRepository;
            _contactItemRoleRepository = contactItemRoleRepository;
            _connectionChannelRepository = connectionChannelRepository;
            _userDefinedTagsRepository = userDefinedTagsRepository;
            _systemStatusTagRepository = systemStatusTagRepository;
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }
        public async Task<PersonRespones> AddPerson(PersonAddRequest? personAddRequest)
        {
            using (Operation.Time("Add person operation for: {PersonName}", personAddRequest?.Name ?? "null"))
            {
                _logger.LogInformation("Executing {MethodName} method at {Timestamp}. Request data: {@PersonAddRequest}",
                    nameof(AddPerson), DateTime.UtcNow, personAddRequest);

                try
                {
                    if (personAddRequest == null)
                    {
                        _logger.LogWarning("AddPerson called with null request parameter");
                        throw new ArgumentNullException(nameof(personAddRequest));
                    }

                    _logger.LogDebug("Validating person add request");
                    ValidationHelpers.ValidationFunction(personAddRequest);

                    _logger.LogDebug("Converting PersonAddRequest to Person entity");
                    var person = personAddRequest.ToPerson();
                    person.PersonId = Guid.NewGuid();

                    if (_currentUserService.UserId == Guid.Empty)
                    {
                        _logger.LogWarning("AddPerson called with no authenticated user context");
                        throw new UnauthorizedAccessException("Cannot add a person without an authenticated user.");
                    }

                    if (!_currentUserService.UserId.HasValue)
                    {
                        throw new UnauthorizedAccessException("No authenticated user context is available.");
                    }

                    person.ApplicationUserId = _currentUserService.UserId.Value;

                    var circles = await ResolveCircles(personAddRequest.Organizations);
                    var channels = await ResolveConnectionChannels(personAddRequest.ConnectionChannels);
                    var tags = await ResolveUserDefinedTags(personAddRequest.UserDefinedTags);
                    var statusTags = await ResolveSystemStatusTags(personAddRequest.SystemStatusTags);

                    foreach (var circle in circles)
                        person.Circles.Add(circle);

                    foreach (var (channel, value) in channels)
                        person.ContactChannels.Add(new ContactChannel
                        {
                            PersonId = person.PersonId,
                            ConnectionChannelId = channel.ConnectionChannelId,
                            Channel = channel,
                            Value = value
                        });

                    foreach (var tag in tags)
                        person.UserDefinedTags.Add(tag);

                    foreach (var statusTag in statusTags)
                        person.SystemStatusTags.Add(statusTag);

                    await ResolveContactItemRoles(person, personAddRequest.CurrentRoles);
                    await ResolveSocialMediaAccounts(person, personAddRequest.SocialMediaAccounts);

                    _logger.LogDebug("Adding new person with ID: {PersonId}, Name: {PersonName}",
                        person.PersonId, person.Name);

                    // Stage changes only
                    await PersonRipository.AddPerson(person);

                    // Single commit for the whole use case
                    await _unitOfWork.SaveChangesAsync();

                    var result = person.ConvertToPersonRespons();
                    result.CountryName = person.Country?.CountryName;

                    _logger.LogInformation("Successfully added new person. ID: {PersonId}, Name: {PersonName}, Country: {CountryName}",
                        result.PersonId, result.Name, result.CountryName);

                    return result;
                }
                catch (ValidationException ex)
                {
                    _logger.LogWarning(ex, "Validation error in AddPerson for request: {@PersonAddRequest}", personAddRequest);
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error occurred in AddPerson for request: {@PersonAddRequest}", personAddRequest);
                    throw;
                }
            }
        }

        /// <summary>
        /// Get-or-create against Circle by name (Organization), batched into a
        /// single query for every requested name instead of one query per name.
        /// </summary>
        private async Task<List<Circle>> ResolveCircles(List<string>? organizationNames)
        {
            var names = (organizationNames ?? new List<string>())
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct()
                .ToList();

            if (names.Count == 0) return new List<Circle>();

            var existing = (await _circleRepository.GetCirclesByNames(names)).ToList();
            var existingNames = existing.Select(c => c.Name).ToHashSet();

            var result = new List<Circle>(existing);
            foreach (var name in names.Where(n => !existingNames.Contains(n)))
            {
                result.Add(new Circle { CircleId = Guid.NewGuid(), Name = name });
            }

            return result;
        }

        private async Task<List<(ConnectionChannel Channel, string? Value)>> ResolveConnectionChannels(List<ContactChannelRequest>? channelRequests)
        {
            var requests = (channelRequests ?? new List<ContactChannelRequest>())
                .Where(r => !string.IsNullOrWhiteSpace(r?.Name))
                .GroupBy(r => r!.Name!.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            if (requests.Count == 0) return new List<(ConnectionChannel, string?)>();

            var names = requests.Select(r => r.Name!.Trim()).ToList();
            var existing = (await _connectionChannelRepository.GetConnectionChannelsByNames(names)).ToList();
            var existingNames = existing.Select(c => c.ConnectionChannelName).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var result = new List<(ConnectionChannel Channel, string? Value)>();
            foreach (var channel in existing)
                result.Add((channel, CleanChannelValue(requests.First(r => string.Equals(r.Name!.Trim(), channel.ConnectionChannelName, StringComparison.OrdinalIgnoreCase)).Value)));

            foreach (var req in requests.Where(r => !existingNames.Contains(r.Name!.Trim())))
            {
                result.Add((new ConnectionChannel
                {
                    ConnectionChannelId = Guid.NewGuid(),
                    ConnectionChannelName = req.Name!.Trim()
                }, CleanChannelValue(req.Value)));
            }

            return result;
        }

        private static string? CleanChannelValue(string? value)
        {
            var clean = (value ?? string.Empty).Trim();
            if (clean.Length == 0) return null;
            return clean.Length > 200 ? clean[..200] : clean;
        }

        private async Task<List<UserDefinedTags>> ResolveUserDefinedTags(List<string>? tagNames)
        {
            var names = (tagNames ?? new List<string>())
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct()
                .ToList();

            if (names.Count == 0) return new List<UserDefinedTags>();

            var existing = (await _userDefinedTagsRepository.GetUserDefinedTagsByNames(names)).ToList();
            var existingNames = existing.Select(t => t.TagName).ToHashSet();

            var result = new List<UserDefinedTags>(existing);
            foreach (var name in names.Where(n => !existingNames.Contains(n)))
            {
                result.Add(new UserDefinedTags { TagId = Guid.NewGuid(), TagName = name });
            }

            return result;
        }

        /// <summary>
        /// SystemStatusTag rows are fixed reference/seed data keyed by enum value
        /// -- there is no "create" path here. Any requested enum with no seeded
        /// row is skipped (with a warning logged inside the repository), rather
        /// than fabricating rows for reference data this service doesn't own.
        /// </summary>
        private async Task<List<SystemStatusTag>> ResolveSystemStatusTags(List<EnSystemStatusTag>? statusTags)
        {
            var ids = (statusTags ?? new List<EnSystemStatusTag>()).Distinct().ToList();

            if (ids.Count == 0) return new List<SystemStatusTag>();

            return (await _systemStatusTagRepository.GetSystemStatusTagsByEnums(ids)).ToList();
        }

        /// <summary>
        /// ContactItemRole is scoped per-person (not a shared lookup), so there's
        /// nothing to batch-fetch for a brand-new person -- every role text becomes
        /// a fresh row tied to this PersonId. No DB call, so no async needed.
        /// </summary>
        private async Task ResolveContactItemRoles(Person person, List<string>? roleNames)
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

        /// <summary>
        /// Social media accounts aren't deduplicated/looked-up -- each submitted
        /// URL becomes its own new row tied to this person.
        /// </summary>
        private async Task ResolveSocialMediaAccounts(Person person, List<SocialMediaAccountAddRequest>? accounts)
        {
            if (accounts == null) return;

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