using ContactsManger.Core.Domain.Entities.EEnums;
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
    public class DemoWorkspaceService : IDemoWorkspaceService
    {
        private readonly IPersonAdderService _adder;
        private readonly IInteractionService _interactions;
        private readonly PersonRepositryContract _persons;
        private readonly IPersonDeleterService _deleter;
        private readonly ICountryGetterService _countries;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<DemoWorkspaceService> _logger;

        public DemoWorkspaceService(
            IPersonAdderService adder,
            IInteractionService interactions,
            PersonRepositryContract persons,
            IPersonDeleterService deleter,
            ICountryGetterService countries,
            ICurrentUserService currentUser,
            ILogger<DemoWorkspaceService> logger)
        {
            _adder = adder;
            _interactions = interactions;
            _persons = persons;
            _deleter = deleter;
            _countries = countries;
            _currentUser = currentUser;
            _logger = logger;
        }

        private sealed record SeedInteraction(
            int DaysAgo,
            EnInteractionType Type,
            string Title,
            string? Description);

        private sealed record SeedPerson(
            string Name,
            string Email,
            string Phone,
            GenderOptions Gender,
            DateTime DateOfBirth,
            string Country,
            string Address,
            string[] Organizations,
            string[] Roles,
            string Origin,
            string Memory,
            string LinkedIn,
            EnSystemStatusTag[] StatusTags,
            string[] UserTags,
            (string Name, string? Value)[] Channels,
            SeedInteraction[] Interactions);

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

                var countries = (await _countries.Countries()).ToDictionary(c => c.CountryName, c => c.CountryId);
                var now = DateTime.UtcNow;

                var people = new SeedPerson[]
                {
                    new("Salma El-Sayed", "salma.elsayed@proceedit.com", "+201012345678", GenderOptions.Female,
                        new DateTime(1991, 3, 14), "Egypt", "Zamalek, Cairo",
                        new[] { "Proceedit" }, new[] { "Product Designer" },
                        "Joined the same week I did — onboarding buddies",
                        "Prefers async voice notes over calls. Her daughter Jana just started KG — ask about it.",
                        "https://www.linkedin.com/in/salma-elsayed",
                        new[] { EnSystemStatusTag.HighPriority }, new[] { "design", "feedback-buddy" },
                        new[] { ("WhatsApp", "+20 101 234 5678"), ("Phone", (string?)null) },
                        new[]
                        {
                            new SeedInteraction(2, EnInteractionType.Message, "Re: onboarding flow v2", "Sent the revised empty states — she loved the illustration direction."),
                            new SeedInteraction(9, EnInteractionType.Call, "Handoff: settings revamp", "Walked through the new IA; she flagged two edge cases in permissions."),
                            new SeedInteraction(16, EnInteractionType.Meeting, "Design crit — checkout", "In-person crit at the Maadi office, agreed on the stepper pattern."),
                            new SeedInteraction(23, EnInteractionType.Message, "Figma link: dashboard widgets", "Left three comments on spacing; she resolved them same day."),
                            new SeedInteraction(31, EnInteractionType.Call, "Portfolio review prep", "Mock review before her ADPList session — gave notes on case-study framing."),
                            new SeedInteraction(44, EnInteractionType.Email, "Intro: Salma <> Dina (research)", "Connected her with Dina for user interviews on the merchant app."),
                            new SeedInteraction(58, EnInteractionType.Meeting, "Q3 planning workshop", "Mapped the design roadmap; she owns onboarding metrics."),
                        }),
                    new("Karim Naguib", "karim.naguib@proceedit.com", "+201098765432", GenderOptions.Male,
                        new DateTime(1987, 7, 2), "Egypt", "Maadi, Cairo",
                        new[] { "Proceedit" }, new[] { "Engineering Manager" },
                        "Interviewed me for the backend role in 2022",
                        "Night owl — best reached after 9pm. Training for the Cairo Runners half marathon.",
                        "https://www.linkedin.com/in/karim-naguib",
                        new[] { EnSystemStatusTag.FollowUp }, new[] { "hiring", "management" },
                        new[] { ("Phone", "+20 109 876 5432"), ("Slack", "@karimn") },
                        new[]
                        {
                            new SeedInteraction(12, EnInteractionType.Call, "Sprint 34 retro follow-up", "Agreed to pilot the new on-call rotation with his squad first."),
                            new SeedInteraction(26, EnInteractionType.Meeting, "Backend hiring: final round debrief", "Aligned on the offer for the senior Go engineer."),
                            new SeedInteraction(41, EnInteractionType.Message, "Staging access for QA", "Sorted the VPN issue for the new QA engineer."),
                            new SeedInteraction(57, EnInteractionType.Email, "Proposal: on-call rotation", "Sent the draft schedule and compensation notes."),
                        }),
                    new("Nourhan Aziz", "nourhan.aziz@gmail.com", "+201112223334", GenderOptions.Female,
                        new DateTime(1990, 11, 25), "Egypt", "Heliopolis, Cairo",
                        new[] { "Sahl Fintech" }, new[] { "Senior Marketing Manager" },
                        "AUC classmate — sat together in ECON 301",
                        "Just promoted to Senior Marketing Manager. Sensitive about work-life balance since the twins arrived.",
                        "https://www.linkedin.com/in/nourhan-aziz",
                        Array.Empty<EnSystemStatusTag>(), new[] { "auc", "friend" },
                        new[] { ("WhatsApp", "+20 111 222 3334"), ("Instagram", "@nourhan.aziz") },
                        new[]
                        {
                            new SeedInteraction(81, EnInteractionType.Message, "Her promotion news", "Congratulated her — a week late, she noticed."),
                            new SeedInteraction(105, EnInteractionType.Call, "Catch-up call", "Talked about the Sahl launch; she offered to intro their partnerships lead."),
                            new SeedInteraction(132, EnInteractionType.Meeting, "Coffee at 30N, Zamalek", "Long overdue catch-up; promised not to wait three months again."),
                            new SeedInteraction(160, EnInteractionType.Message, "Eid greetings", "Exchanged greetings, said we must meet after the holiday."),
                        }),
                    new("Mohamed Farouk", "m.farouk@gmail.com", "+201055566677", GenderOptions.Male,
                        new DateTime(1975, 5, 30), "Egypt", "Heliopolis, Cairo",
                        new[] { "Ex: NilePay" }, new[] { "Former VP Engineering" },
                        "My manager at NilePay, 2019–2021",
                        "Moved to Riyadh in March. Mentor since 2019 — always ask about his olive trees in Fayoum.",
                        "https://www.linkedin.com/in/mohamed-farouk",
                        new[] { EnSystemStatusTag.ShouldReply }, new[] { "mentor" },
                        new[] { ("Phone", "+20 105 556 6677"), ("Email", (string?)null) },
                        new[]
                        {
                            new SeedInteraction(118, EnInteractionType.Call, "Career advice: staff vs manager track", "He pushed me toward staff for now; sent two books afterwards."),
                            new SeedInteraction(150, EnInteractionType.Email, "Recommendation letter request", "Asked for a reference for the conference talk application."),
                            new SeedInteraction(190, EnInteractionType.Meeting, "Breakfast in Heliopolis", "Farewell breakfast before his Riyadh move."),
                        }),
                    new("Layla Hassan", "layla.hassan@gmail.com", "+201044455566", GenderOptions.Female,
                        new DateTime(1994, 9, 8), "Egypt", "Nasr City, Cairo",
                        Array.Empty<string>(), new[] { "Pharmacist" },
                        "Cousin — aunt Mona's daughter",
                        "Vegetarian. Planning a Red Sea diving trip in October — wants company.",
                        "https://www.linkedin.com/in/layla-hassan",
                        Array.Empty<EnSystemStatusTag>(), new[] { "family" },
                        new[] { ("WhatsApp", "+20 104 445 5566"), ("Phone", (string?)null) },
                        new[]
                        {
                            new SeedInteraction(3, EnInteractionType.Call, "Weekend plans at Sokhna", "Coordinating the chalet booking with the cousins."),
                            new SeedInteraction(10, EnInteractionType.Message, "Jana's birthday photos", "Sent the album; she wants prints for aunt Mona."),
                            new SeedInteraction(21, EnInteractionType.Meeting, "Family lunch in Nasr City", "Monthly family lunch, discussed Eid plans."),
                        }),
                    new("Omar Khalil", "omar.khalil@gmail.com", "+201077788899", GenderOptions.Male,
                        new DateTime(1989, 1, 19), "Egypt", "Downtown, Cairo",
                        new[] { "Proceedit", "Cairo Product Community" }, new[] { "Community Organizer" },
                        "Spoke after his talk at RiseUp, introduced by Salma",
                        "Knows everyone in the Cairo tech scene. Remembers birthdays — reciprocate.",
                        "https://www.linkedin.com/in/omar-khalil",
                        new[] { EnSystemStatusTag.Urgent }, new[] { "community", "connector" },
                        new[] { ("WhatsApp", "+20 107 778 8899"), ("LinkedIn", "omar-khalil") },
                        new[]
                        {
                            new SeedInteraction(6, EnInteractionType.Message, "Panel invite: AI in product", "Invited me to moderate the June panel — said yes."),
                            new SeedInteraction(20, EnInteractionType.Meeting, "Meetup #42 retro", "Retro at the GrEEK Campus; 180 attendees, best turnout yet."),
                            new SeedInteraction(35, EnInteractionType.Call, "Sponsor intro: Proceedit <> community", "Pitched the sponsorship package to our marketing lead."),
                            new SeedInteraction(49, EnInteractionType.Email, "Speaker lineup for June", "Confirmed three speakers, one slot still open."),
                            new SeedInteraction(66, EnInteractionType.Message, "Venue hunt in Downtown", "Scouting a bigger venue for the anniversary edition."),
                        }),
                    new("Jonas Weber", "jonas.weber@gmail.com", "+491512345678", GenderOptions.Male,
                        new DateTime(1988, 12, 3), "Germany", "Kreuzberg, Berlin",
                        new[] { "Ex: NilePay" }, new[] { "iOS Engineer" },
                        "Worked together at NilePay before he moved to Berlin",
                        "One hour behind Cairo in winter. Learning Arabic — delights in practicing on calls.",
                        "https://www.linkedin.com/in/jonas-weber",
                        Array.Empty<EnSystemStatusTag>(), new[] { "berlin", "ex-colleague" },
                        new[] { ("WhatsApp", "+49 151 2345678"), ("Phone", (string?)null) },
                        new[]
                        {
                            new SeedInteraction(45, EnInteractionType.Call, "Time-zone-friendly catch-up", "He is settling well; comparing notes on SwiftUI adoption."),
                            new SeedInteraction(75, EnInteractionType.Message, "Marathon training update", "Running the Berlin half — swapped training plans."),
                            new SeedInteraction(110, EnInteractionType.Email, "Referral: iOS role at a Berlin fintech", "Asked if I know anyone looking; forwarded Karim's ex-teammate."),
                        }),
                    new("Aisha Bello", "aisha.bello@gmail.com", "+201033344455", GenderOptions.Female,
                        new DateTime(1985, 6, 11), "Egypt", "New Cairo, Cairo",
                        new[] { "Gouna Resorts" }, new[] { "Digital Director" },
                        "Client — led their booking flow redesign",
                        "Pays net-30, always on time. Prefers formal emails; CC her assistant Mona.",
                        "https://www.linkedin.com/in/aisha-bello",
                        new[] { EnSystemStatusTag.FollowUp }, new[] { "client" },
                        new[] { ("Email", (string?)null), ("Phone", "+20 103 334 4455") },
                        new[]
                        {
                            new SeedInteraction(15, EnInteractionType.Meeting, "UAT sign-off session", "Signed off with two minor fixes; go-live confirmed."),
                            new SeedInteraction(33, EnInteractionType.Call, "Invoice + next phase scoping", "Agreed the phase-2 scope; proposal due end of month."),
                            new SeedInteraction(52, EnInteractionType.Email, "Contract renewal draft", "Sent the renewal with the maintenance clause updated."),
                        }),
                    new("Tarek Mansour", "tarek.mansour@gmail.com", "+201066677788", GenderOptions.Male,
                        new DateTime(1978, 2, 22), "Egypt", "Sheikh Zayed, Giza",
                        new[] { "Independent" }, new[] { "Angel Investor" },
                        "Intro coffee after the RiseUp summit",
                        "Angel investor, ex-telecom. Speaks fast, decides fast — come with numbers.",
                        "https://www.linkedin.com/in/tarek-mansour",
                        Array.Empty<EnSystemStatusTag>(), new[] { "advisor", "investor" },
                        new[] { ("Phone", "+20 106 667 7788"), ("LinkedIn", "tarek-mansour") },
                        new[]
                        {
                            new SeedInteraction(25, EnInteractionType.Meeting, "Intro coffee after RiseUp", "Shared the deck; he asked for unit economics before a second meeting."),
                        }),
                    new("Dina Samir", "dina.samir@gmail.com", "+201022233344", GenderOptions.Female,
                        new DateTime(1993, 8, 17), "Egypt", "Mohandessin, Giza",
                        new[] { "Cairo UX Guild" }, new[] { "UX Researcher" },
                        "Sister",
                        "Brutally honest feedback — the person to sanity-check big decisions with.",
                        "https://www.linkedin.com/in/dina-samir",
                        Array.Empty<EnSystemStatusTag>(), new[] { "family" },
                        new[] { ("WhatsApp", "+20 102 223 3344"), ("Phone", (string?)null) },
                        new[]
                        {
                            new SeedInteraction(1, EnInteractionType.Message, "Lunch tomorrow?", "Meeting at Kazouza in Mohandessin at 1pm."),
                            new SeedInteraction(5, EnInteractionType.Call, "Mom's birthday gift plan", "Splitting the air-fryer; I order, she wraps."),
                            new SeedInteraction(12, EnInteractionType.Meeting, "Dinner in Zamalek", "Tried the new Levantine place; debriefed her job interview."),
                            new SeedInteraction(19, EnInteractionType.Message, "Apartment viewing in New Cairo", "Sent photos of the Rehab flat — overpriced, keep looking."),
                            new SeedInteraction(27, EnInteractionType.Call, "Weekly catch-up", "Talked her through the promotion calibration outcome."),
                        }),
                    new("Yasmine Riad", "yasmine.riad@gmail.com", "+201099900011", GenderOptions.Female,
                        new DateTime(1996, 4, 5), "Egypt", "Heliopolis, Cairo",
                        new[] { "TalentBridge" }, new[] { "Tech Recruiter" },
                        "Reached out on LinkedIn about a role",
                        "Recruiter who reached out on LinkedIn. Verify the company before engaging further.",
                        "https://www.linkedin.com/in/yasmine-riad",
                        new[] { EnSystemStatusTag.HaveNotReplied }, new[] { "recruiting" },
                        new[] { ("LinkedIn", "yasmine-riad"), ("Email", (string?)null) },
                        new[]
                        {
                            new SeedInteraction(9, EnInteractionType.Email, "Role: Senior PM at a Gulf fintech", "Interesting on paper; asked which company before sharing my CV."),
                        }),
                };

                int created = 0;
                foreach (var person in people)
                {
                    var added = await _adder.AddPerson(new PersonAddRequest
                    {
                        Name = person.Name,
                        email = person.Email,
                        phone = person.Phone,
                        Gender = person.Gender,
                        DateOfBirth = person.DateOfBirth,
                        Address = person.Address,
                        CountryId = countries[person.Country],
                        NewsLetter = false,
                        ContextMemory = person.Memory,
                        Origin = person.Origin,
                        LinkedInProfile = person.LinkedIn,
                        Organizations = person.Organizations.ToList(),
                        CurrentRoles = person.Roles.ToList(),
                        ConnectionChannels = person.Channels.Select(c => new ContactChannelRequest { Name = c.Name, Value = c.Value }).ToList(),
                        SystemStatusTags = person.StatusTags.ToList(),
                        UserDefinedTags = person.UserTags.ToList()
                    });

                    foreach (var interaction in person.Interactions)
                    {
                        await _interactions.LogAsync(new InteractionAddRequest
                        {
                            PersonId = added.PersonId,
                            TimeOfInteraction = now.AddDays(-interaction.DaysAgo),
                            InteractionType = interaction.Type,
                            InteractionTitle = interaction.Title,
                            InteractionDescription = interaction.Description
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
                    "salma.elsayed@proceedit.com", "karim.naguib@proceedit.com",
                    "nourhan.aziz@gmail.com", "m.farouk@gmail.com",
                    "layla.hassan@gmail.com", "omar.khalil@gmail.com",
                    "jonas.weber@gmail.com", "aisha.bello@gmail.com",
                    "tarek.mansour@gmail.com", "dina.samir@gmail.com",
                    "yasmine.riad@gmail.com"
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
