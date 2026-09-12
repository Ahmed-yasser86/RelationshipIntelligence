using ContactsManger.Core.Domain.Entities.EEnums;
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
    public class DemoWorkspaceService : IDemoWorkspaceService
    {
        private readonly IPersonQuickAdderService _quickAdd;
        private readonly IInteractionService _interactions;
        private readonly PersonRepositryContract _persons;
        private readonly IPersonDeleterService _deleter;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<DemoWorkspaceService> _logger;

        public DemoWorkspaceService(
            IPersonQuickAdderService quickAdd,
            IInteractionService interactions,
            PersonRepositryContract persons,
            IPersonDeleterService deleter,
            ICurrentUserService currentUser,
            ILogger<DemoWorkspaceService> logger)
        {
            _quickAdd = quickAdd;
            _interactions = interactions;
            _persons = persons;
            _deleter = deleter;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<int> SeedAsync()
        {
            using (Operation.Time("Seed demo workspace"))
            {
                var userId = _currentUser.UserId;
                if (userId == null || userId == Guid.Empty)
                    throw new UnauthorizedAccessException("Cannot seed a demo workspace without an authenticated user.");

                var existing = await _persons.GetAllPersons();
                if (existing.Count() >= 5)
                    throw new InvalidOperationException("Demo seeding is only available for nearly empty workspaces.");

                var now = DateTime.UtcNow;
                var people = new[]
                {
                    ("Maya Chen", "maya.chen@example.com", new[] { "Northwind Studio" }, new[] { "Design Lead" }, "Met at a design systems meetup", new[] { 2, 9, 16 }),
                    ("Tomas Rivera", "tomas.rivera@example.com", new[] { "Northwind Studio" }, new[] { "Engineer" }, "Introduced by Maya", new[] { 5, 19 }),
                    ("Priya Nair", "priya.nair@example.com", new[] { "Harbor Consulting" }, new[] { "Partner" }, "Former client", new[] { 34, 48, 62 }),
                    ("Samuel Okafor", "samuel.okafor@example.com", new[] { "Harbor Consulting" }, new[] { "Analyst" }, "Workshop attendee", new[] { 41 }),
                    ("Lena Fischer", "lena.fischer@example.com", new[] { "Beacon Fund" }, new[] { "Investor" }, "Cold outreach, warm reply", new[] { 88, 104 }),
                    ("Omar Haddad", "omar.haddad@example.com", new[] { "Beacon Fund" }, new[] { "Advisor" }, "Conference panel", new[] { 97, 121 }),
                    ("June Park", "june.park@example.com", new[] { "Northwind Studio", "Beacon Fund" }, new[] { "Advisor" }, "Connects two circles", new[] { 55, 70 }),
                    ("Alex Morgan", "alex.morgan@example.com", new string[0], new[] { "Writer" }, "Just met", new int[0]),
                };

                int created = 0;
                foreach (var (name, email, orgs, roles, origin, daysAgo) in people)
                {
                    var added = await _quickAdd.QuickAddPerson(new PersonQuickAddRequest
                    {
                        Name = name,
                        email = email,
                        Organizations = orgs.ToList(),
                        CurrentRoles = roles.ToList(),
                        Origin = origin
                    });

                    foreach (var days in daysAgo)
                    {
                        await _interactions.LogAsync(new InteractionAddRequest
                        {
                            PersonId = added.PersonId,
                            TimeOfInteraction = now.AddDays(-days),
                            InteractionType = EnInteractionType.Email,
                            InteractionTitle = "Introductory conversation"
                        });
                    }
                    created++;
                }

                _logger.LogInformation("Seeded demo workspace with {Count} contacts", created);
                return created;
            }
        }

        /// <summary>
        /// Removes exactly the contacts created by <see cref="SeedAsync"/>,
        /// identified by their fixed demo email addresses. Never touches
        /// any other row, so it is safe to offer as "remove demo data".
        /// </summary>
        public async Task<int> ClearAsync()
        {
            using (Operation.Time("Clear demo workspace"))
            {
                var userId = _currentUser.UserId;
                if (userId == null || userId == Guid.Empty)
                    throw new UnauthorizedAccessException("Cannot clear a demo workspace without an authenticated user.");

                var people = (await _persons.GetAllPersons()).ToList();
                var demoEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "maya.chen@example.com", "tomas.rivera@example.com",
                    "priya.nair@example.com", "samuel.okafor@example.com",
                    "lena.fischer@example.com", "omar.haddad@example.com",
                    "june.park@example.com", "alex.morgan@example.com"
                };

                int removed = 0;
                foreach (var person in people)
                {
                    if (person != null && person.email != null && demoEmails.Contains(person.email))
                    {
                        if (await _deleter.DeletePersonByPersonId(person.PersonId))
                            removed++;
                    }
                }
                return removed;
            }
        }
    }
}
