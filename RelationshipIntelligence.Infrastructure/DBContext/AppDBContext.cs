using ContactsManger.Core.Domain.Entities;
using ContactsManger.Core.Domain.Entities.EEnums;
using ContactsManger.Core.Domain.IdentityEntities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace Entities
{
    public class AppDBContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
    {

        private readonly Guid? _currentUserId;

        public AppDBContext(DbContextOptions<AppDBContext> options, ServiceContracts.ICurrentUserService currentUserService)
            : base(options)
        {
            _currentUserId = currentUserService.UserId;
        }

        public virtual DbSet<Person> Persons { get; set; }
        public virtual DbSet<Country> Countries { get; set; }
        public virtual DbSet<ContactItemRole> ContactItemRoles { get; set; }
        public virtual DbSet<ConnectionChannel> ConnectionChannels { get; set; }
        public virtual DbSet<UserDefinedTags> UserDefinedTags { get; set; }
        public virtual DbSet<SystemStatusTag> SystemStatusTags { get; set; }
        public virtual DbSet<Circle> Circles { get; set; }
        public virtual DbSet<ContactChannel> ContactChannels { get; set; }
        public virtual DbSet<Note> Notes { get; set; }
        public virtual DbSet<Interaction> Interactions { get; set; }
        public virtual DbSet<SocialMediaAccount> SocialMediaAccounts { get; set; }
        public virtual DbSet<RelationshipState> RelationshipStates { get; set; }
        public virtual DbSet<RelationshipStateSnapshot> RelationshipStateSnapshots { get; set; }
        public virtual DbSet<DigestDelivery> DigestDeliveries { get; set; }
        public virtual DbSet<DigestMetric> DigestMetrics { get; set; }
        public virtual DbSet<DigestPreference> DigestPreferences { get; set; }
        public virtual DbSet<RelationshipMemoryEntry> RelationshipMemoryEntries { get; set; }
        public virtual DbSet<RelationshipEvent> RelationshipEvents { get; set; }
        public virtual DbSet<AiProviderSettings> AiProviderSettings { get; set; }
        public virtual DbSet<Meeting> Meetings { get; set; }
        public virtual DbSet<MeetingPerson> MeetingPersons { get; set; }
        public virtual DbSet<MeetingFinding> MeetingFindings { get; set; }
        public virtual DbSet<MeetingBrief> MeetingBriefs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Country>().ToTable("Countries");
            modelBuilder.Entity<Person>().ToTable("Persons");
            modelBuilder.Entity<ContactItemRole>().ToTable("ContactItemRoles");
            modelBuilder.Entity<ConnectionChannel>().ToTable("ConnectionChannels");
            modelBuilder.Entity<SystemStatusTag>().ToTable("SystemStatusTags");
            modelBuilder.Entity<Circle>().ToTable("Circles");
            modelBuilder.Entity<Note>().ToTable("Notes");
            modelBuilder.Entity<Interaction>().ToTable("Interactions");
            modelBuilder.Entity<SocialMediaAccount>().ToTable("SocialMediaAccounts");
            modelBuilder.Entity<RelationshipState>().ToTable("RelationshipStates");
            modelBuilder.Entity<RelationshipState>()
                .HasIndex(s => new { s.ApplicationUserId, s.PersonId })
                .IsUnique();
            modelBuilder.Entity<RelationshipStateSnapshot>().ToTable("RelationshipStateSnapshots");
            modelBuilder.Entity<RelationshipStateSnapshot>()
                .HasIndex(s => new { s.ApplicationUserId, s.PersonId, s.TakenAtUtc });

            modelBuilder.Entity<Person>()
                .HasQueryFilter(p => p.ApplicationUserId == _currentUserId && !p.IsDeleted);
            modelBuilder.Entity<RelationshipState>()
                .HasQueryFilter(s => s.ApplicationUserId == _currentUserId);
            modelBuilder.Entity<RelationshipStateSnapshot>()
                .HasQueryFilter(s => s.ApplicationUserId == _currentUserId);
            modelBuilder.Entity<DigestDelivery>().ToTable("DigestDeliveries");
            modelBuilder.Entity<DigestDelivery>()
                .HasIndex(d => new { d.ApplicationUserId, d.WeekStartUtc })
                .IsUnique();
            modelBuilder.Entity<DigestDelivery>()
                .HasQueryFilter(d => d.ApplicationUserId == _currentUserId);
            modelBuilder.Entity<DigestMetric>().ToTable("DigestMetrics");
            modelBuilder.Entity<DigestMetric>()
                .HasQueryFilter(m => m.ApplicationUserId == _currentUserId);
            modelBuilder.Entity<DigestPreference>().ToTable("DigestPreferences");
            modelBuilder.Entity<DigestPreference>()
                .HasQueryFilter(p => p.ApplicationUserId == _currentUserId);
            modelBuilder.Entity<RelationshipMemoryEntry>().ToTable("RelationshipMemoryEntries");
            modelBuilder.Entity<RelationshipMemoryEntry>()
                .HasIndex(e => new { e.ApplicationUserId, e.PersonId, e.Status });
            modelBuilder.Entity<RelationshipMemoryEntry>()
                .HasQueryFilter(e => e.ApplicationUserId == _currentUserId);
            modelBuilder.Entity<RelationshipMemoryEntry>()
                .HasOne(e => e.Person)
                .WithMany()
                .HasForeignKey(e => e.PersonId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<RelationshipEvent>().ToTable("RelationshipEvents");
            modelBuilder.Entity<RelationshipEvent>()
                .HasIndex(e => new { e.ApplicationUserId, e.OccursOn });
            modelBuilder.Entity<RelationshipEvent>()
                .HasQueryFilter(e => e.ApplicationUserId == _currentUserId);
            modelBuilder.Entity<RelationshipEvent>()
                .HasOne(e => e.Person)
                .WithMany()
                .HasForeignKey(e => e.PersonId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<AiProviderSettings>().ToTable("AiProviderSettings");
            modelBuilder.Entity<AiProviderSettings>()
                .HasQueryFilter(s => s.ApplicationUserId == _currentUserId);
            modelBuilder.Entity<Meeting>().ToTable("Meetings");
            modelBuilder.Entity<Meeting>()
                .HasIndex(m => new { m.ApplicationUserId, m.Status });
            modelBuilder.Entity<Meeting>()
                .HasQueryFilter(m => m.ApplicationUserId == _currentUserId);
            modelBuilder.Entity<MeetingPerson>().ToTable("MeetingPersons");
            modelBuilder.Entity<MeetingPerson>()
                .HasIndex(p => p.MeetingId);
            modelBuilder.Entity<MeetingPerson>()
                .HasOne(p => p.Meeting)
                .WithMany(m => m.People)
                .HasForeignKey(p => p.MeetingId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<MeetingPerson>()
                .HasOne(p => p.MappedPerson)
                .WithMany()
                .HasForeignKey(p => p.MappedPersonId)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<MeetingFinding>().ToTable("MeetingFindings");
            modelBuilder.Entity<MeetingFinding>()
                .HasIndex(f => new { f.MeetingId, f.MappedPersonId });
            modelBuilder.Entity<MeetingFinding>()
                .HasOne(f => f.Meeting)
                .WithMany(m => m.Findings)
                .HasForeignKey(f => f.MeetingId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<MeetingFinding>()
                .HasOne(f => f.MappedPerson)
                .WithMany()
                .HasForeignKey(f => f.MappedPersonId)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<MeetingBrief>().ToTable("MeetingBriefs");
            modelBuilder.Entity<MeetingBrief>()
                .HasIndex(b => b.MeetingId)
                .IsUnique();
            modelBuilder.Entity<MeetingBrief>()
                .HasOne(b => b.Meeting)
                .WithOne(m => m.Brief)
                .HasForeignKey<MeetingBrief>(b => b.MeetingId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Person>()
                .HasOne(p => p.ApplicationUser)
                .WithMany()
                .HasForeignKey(p => p.ApplicationUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ==========================================
            // 2. RELATIONSHIPS
            // ==========================================
            modelBuilder.Entity<ContactChannel>(e =>
            {
                e.ToTable("PersonConnectionChannels");
                e.HasKey(x => new { x.PersonId, x.ConnectionChannelId });
                e.Property(x => x.PersonId).HasColumnName("PeoplePersonId");
                e.Property(x => x.ConnectionChannelId).HasColumnName("ConnectionChannelsConnectionChannelId");
                e.Property(x => x.Value).HasMaxLength(200);
                e.HasOne(x => x.Person)
                    .WithMany(p => p.ContactChannels)
                    .HasForeignKey(x => x.PersonId);
                e.HasOne(x => x.Channel)
                    .WithMany(c => c.ContactChannels)
                    .HasForeignKey(x => x.ConnectionChannelId);
            });

            modelBuilder.Entity<Person>()
                .HasMany(p => p.UserDefinedTags)
                .WithMany(t => t.People);

            modelBuilder.Entity<Person>()
                .HasMany(p => p.SystemStatusTags)
                .WithMany(t => t.People);

            modelBuilder.Entity<Person>()
                .HasMany(p => p.Circles)
                .WithMany(c => c.People)
                .UsingEntity(j => j.ToTable("PersonCircles"));

            // Notes and Interactions are one-to-many (a Note/Interaction
            // belongs to exactly one Person) -- no join table.
            modelBuilder.Entity<Person>()
                .HasMany(p => p.Notes)
                .WithOne(n => n.Person)
                .HasForeignKey(n => n.PersonId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Person>()
                .HasMany(p => p.Interactions)
                .WithOne(i => i.Person)
                .HasForeignKey(i => i.PersonId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SocialMediaAccount>()
                .HasMany(s => s.People)
                .WithMany(p => p.OtherSocialMediaAccounts)
                .UsingEntity(j => j.ToTable("PersonSocialMediaAccounts"));

            // ==========================================
            // 3. CLEAN GUID IDENTIFIERS (10 per entity)
            // ==========================================
            var countries = new[] {
                Guid.Parse("c0000001-0000-0000-0000-000000000000"), Guid.Parse("c0000002-0000-0000-0000-000000000000"), Guid.Parse("c0000003-0000-0000-0000-000000000000"), Guid.Parse("c0000004-0000-0000-0000-000000000000"), Guid.Parse("c0000005-0000-0000-0000-000000000000"),
                Guid.Parse("c0000006-0000-0000-0000-000000000000"), Guid.Parse("c0000007-0000-0000-0000-000000000000"), Guid.Parse("c0000008-0000-0000-0000-000000000000"), Guid.Parse("c0000009-0000-0000-0000-000000000000"), Guid.Parse("c000000a-0000-0000-0000-000000000000")
            };

            var channels = new[] {
                Guid.Parse("d0000001-0000-0000-0000-000000000000"), Guid.Parse("d0000002-0000-0000-0000-000000000000"), Guid.Parse("d0000003-0000-0000-0000-000000000000"), Guid.Parse("d0000004-0000-0000-0000-000000000000"), Guid.Parse("d0000005-0000-0000-0000-000000000000"),
                Guid.Parse("d0000006-0000-0000-0000-000000000000"), Guid.Parse("d0000007-0000-0000-0000-000000000000"), Guid.Parse("d0000008-0000-0000-0000-000000000000"), Guid.Parse("d0000009-0000-0000-0000-000000000000"), Guid.Parse("d000000a-0000-0000-0000-000000000000")
            };

            var tags = new[] {
                Guid.Parse("e0000001-0000-0000-0000-000000000000"), Guid.Parse("e0000002-0000-0000-0000-000000000000"), Guid.Parse("e0000003-0000-0000-0000-000000000000"), Guid.Parse("e0000004-0000-0000-0000-000000000000"), Guid.Parse("e0000005-0000-0000-0000-000000000000"),
                Guid.Parse("e0000006-0000-0000-0000-000000000000"), Guid.Parse("e0000007-0000-0000-0000-000000000000"), Guid.Parse("e0000008-0000-0000-0000-000000000000"), Guid.Parse("e0000009-0000-0000-0000-000000000000"), Guid.Parse("e000000a-0000-0000-0000-000000000000")
            };

            var circles = new[] {
                Guid.Parse("f0000001-0000-0000-0000-000000000000"), Guid.Parse("f0000002-0000-0000-0000-000000000000"), Guid.Parse("f0000003-0000-0000-0000-000000000000"), Guid.Parse("f0000004-0000-0000-0000-000000000000"), Guid.Parse("f0000005-0000-0000-0000-000000000000"),
                Guid.Parse("f0000006-0000-0000-0000-000000000000"), Guid.Parse("f0000007-0000-0000-0000-000000000000"), Guid.Parse("f0000008-0000-0000-0000-000000000000"), Guid.Parse("f0000009-0000-0000-0000-000000000000"), Guid.Parse("f000000a-0000-0000-0000-000000000000")
            };

            var notes = new[] {
                Guid.Parse("b0000001-0000-0000-0000-000000000000"), Guid.Parse("b0000002-0000-0000-0000-000000000000"), Guid.Parse("b0000003-0000-0000-0000-000000000000"), Guid.Parse("b0000004-0000-0000-0000-000000000000"), Guid.Parse("b0000005-0000-0000-0000-000000000000"),
                Guid.Parse("b0000006-0000-0000-0000-000000000000"), Guid.Parse("b0000007-0000-0000-0000-000000000000"), Guid.Parse("b0000008-0000-0000-0000-000000000000"), Guid.Parse("b0000009-0000-0000-0000-000000000000"), Guid.Parse("b000000a-0000-0000-0000-000000000000")
            };

            var interactions = new[] {
                Guid.Parse("a0000001-0000-0000-0000-000000000000"), Guid.Parse("a0000002-0000-0000-0000-000000000000"), Guid.Parse("a0000003-0000-0000-0000-000000000000"), Guid.Parse("a0000004-0000-0000-0000-000000000000"), Guid.Parse("a0000005-0000-0000-0000-000000000000"),
                Guid.Parse("a0000006-0000-0000-0000-000000000000"), Guid.Parse("a0000007-0000-0000-0000-000000000000"), Guid.Parse("a0000008-0000-0000-0000-000000000000"), Guid.Parse("a0000009-0000-0000-0000-000000000000"), Guid.Parse("a000000a-0000-0000-0000-000000000000")
            };

            var persons = new[] {
                Guid.Parse("10000001-0000-0000-0000-000000000000"), Guid.Parse("10000002-0000-0000-0000-000000000000"), Guid.Parse("10000003-0000-0000-0000-000000000000"), Guid.Parse("10000004-0000-0000-0000-000000000000"), Guid.Parse("10000005-0000-0000-0000-000000000000"),
                Guid.Parse("10000006-0000-0000-0000-000000000000"), Guid.Parse("10000007-0000-0000-0000-000000000000"), Guid.Parse("10000008-0000-0000-0000-000000000000"), Guid.Parse("10000009-0000-0000-0000-000000000000"), Guid.Parse("1000000a-0000-0000-0000-000000000000")
            };

            var roles = new[] {
                Guid.Parse("20000001-0000-0000-0000-000000000000"), Guid.Parse("20000002-0000-0000-0000-000000000000"), Guid.Parse("20000003-0000-0000-0000-000000000000"), Guid.Parse("20000004-0000-0000-0000-000000000000"), Guid.Parse("20000005-0000-0000-0000-000000000000"),
                Guid.Parse("20000006-0000-0000-0000-000000000000"), Guid.Parse("20000007-0000-0000-0000-000000000000"), Guid.Parse("20000008-0000-0000-0000-000000000000"), Guid.Parse("20000009-0000-0000-0000-000000000000"), Guid.Parse("2000000a-0000-0000-0000-000000000000")
            };

            var smAccounts = new[]
            {
                Guid.Parse("90000001-0000-0000-0000-000000000000"),
                Guid.Parse("90000002-0000-0000-0000-000000000000"),
                Guid.Parse("90000003-0000-0000-0000-000000000000"),
                Guid.Parse("90000004-0000-0000-0000-000000000000"),
                Guid.Parse("90000005-0000-0000-0000-000000000000"),
                Guid.Parse("90000006-0000-0000-0000-000000000000"),
                Guid.Parse("90000007-0000-0000-0000-000000000000"),
                Guid.Parse("90000008-0000-0000-0000-000000000000"),
                Guid.Parse("90000009-0000-0000-0000-000000000000"),
                Guid.Parse("9000000a-0000-0000-0000-000000000000")
            };

            // ==========================================
            // 3b. SEED TEST USER + ROLE
            // ==========================================
            var seedUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var seedRoleId = Guid.Parse("00000000-0000-0000-0000-000000000002");

            modelBuilder.Entity<ApplicationRole>().HasData(
                new ApplicationRole
                {
                    Id = seedRoleId,
                    Name = "User",
                    NormalizedName = "USER"
                }
            );

            modelBuilder.Entity<ApplicationUser>().HasData(
                new ApplicationUser
                {
                    Id = seedUserId,
                    UserName = "testuser@contactsmanager.dev",
                    NormalizedUserName = "TESTUSER@CONTACTSMANAGER.DEV",
                    Email = "testuser@contactsmanager.dev",
                    NormalizedEmail = "TESTUSER@CONTACTSMANAGER.DEV",
                    EmailConfirmed = true,
                    PersonName = "Test User",
                    SecurityStamp = "STATIC-SEED-SECURITY-STAMP-0001",
                    ConcurrencyStamp = "STATIC-SEED-CONCURRENCY-STAMP-0001",
                    PasswordHash = "AQAAAAIAAYagAAAAEP9N9FETj6XBlr2nCwBktgDVpDvbmOXLKGisPywjI8prNBnxoqoHbJ5eYAlzc98QUw=="
                }
            );

            // pass is test@123456
            modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<Guid>>().HasData(
                new Microsoft.AspNetCore.Identity.IdentityUserRole<Guid>
                {
                    UserId = seedUserId,
                    RoleId = seedRoleId
                }
            );

            // ==========================================
            // 4. LOOKUP SEED DATA
            // ==========================================

            modelBuilder.Entity<Country>().HasData(
                new Country { CountryId = countries[0], CountryName = "Egypt" },
                new Country { CountryId = countries[1], CountryName = "Australia" },
                new Country { CountryId = countries[2], CountryName = "USA" },
                new Country { CountryId = countries[3], CountryName = "Japan" },
                new Country { CountryId = countries[4], CountryName = "Canada" },
                new Country { CountryId = countries[5], CountryName = "United Kingdom" },
                new Country { CountryId = countries[6], CountryName = "Germany" },
                new Country { CountryId = countries[7], CountryName = "France" },
                new Country { CountryId = countries[8], CountryName = "China" },
                new Country { CountryId = countries[9], CountryName = "Norway" }
            );

            modelBuilder.Entity<ConnectionChannel>().HasData(
                new ConnectionChannel { ConnectionChannelId = channels[0], ConnectionChannelName = "LinkedIn" },
                new ConnectionChannel { ConnectionChannelId = channels[1], ConnectionChannelName = "Twitter (X)" },
                new ConnectionChannel { ConnectionChannelId = channels[2], ConnectionChannelName = "GitHub" },
                new ConnectionChannel { ConnectionChannelId = channels[3], ConnectionChannelName = "Email" },
                new ConnectionChannel { ConnectionChannelId = channels[4], ConnectionChannelName = "WhatsApp" },
                new ConnectionChannel { ConnectionChannelId = channels[5], ConnectionChannelName = "Discord" },
                new ConnectionChannel { ConnectionChannelId = channels[6], ConnectionChannelName = "Slack" },
                new ConnectionChannel { ConnectionChannelId = channels[7], ConnectionChannelName = "Zoom" },
                new ConnectionChannel { ConnectionChannelId = channels[8], ConnectionChannelName = "Microsoft Teams" },
                new ConnectionChannel { ConnectionChannelId = channels[9], ConnectionChannelName = "Telegram" }
            );

            modelBuilder.Entity<SystemStatusTag>().HasData(
                new SystemStatusTag { StatusTagId = EnSystemStatusTag.FollowUp, Name = "Follow Up", Description = "Needs follow up" },
                new SystemStatusTag { StatusTagId = EnSystemStatusTag.Urgent, Name = "Urgent", Description = "Urgent follow up required" },
                new SystemStatusTag { StatusTagId = EnSystemStatusTag.ContactedRecently, Name = "Recently Contacted", Description = "Spoke to them lately" },
                new SystemStatusTag { StatusTagId = EnSystemStatusTag.ShouldReply, Name = "Should Reply", Description = "Owe them a response" },
                new SystemStatusTag { StatusTagId = EnSystemStatusTag.AlreadyReplied, Name = "Already Replied", Description = "Awaiting their response" },
                new SystemStatusTag { StatusTagId = EnSystemStatusTag.HaveNotReplied, Name = "Ignored Me", Description = "They haven't replied" },
                new SystemStatusTag { StatusTagId = EnSystemStatusTag.Ignored, Name = "Ignored", Description = "Skipped intentionally" },
                new SystemStatusTag { StatusTagId = EnSystemStatusTag.HighPriority, Name = "High Priority", Description = "Immediate attention needed" },
                new SystemStatusTag { StatusTagId = EnSystemStatusTag.LowPriority, Name = "Low Priority", Description = "Get to it later" },
                new SystemStatusTag { StatusTagId = EnSystemStatusTag.ModeratePriority, Name = "Moderate Priority", Description = "Standard priority attention" }
            );

            modelBuilder.Entity<UserDefinedTags>().HasData(
                new UserDefinedTags { TagId = tags[0], TagName = "Software Engineering" },
                new UserDefinedTags { TagId = tags[1], TagName = "Economics Research" },
                new UserDefinedTags { TagId = tags[2], TagName = "Capital University" },
                new UserDefinedTags { TagId = tags[3], TagName = "Mentors" },
                new UserDefinedTags { TagId = tags[4], TagName = "Backend Devs" },
                new UserDefinedTags { TagId = tags[5], TagName = "Data Science" },
                new UserDefinedTags { TagId = tags[6], TagName = "Sociology" },
                new UserDefinedTags { TagId = tags[7], TagName = "Cairo History" },
                new UserDefinedTags { TagId = tags[8], TagName = "Networking" },
                new UserDefinedTags { TagId = tags[9], TagName = "Friends" }
            );

            modelBuilder.Entity<Circle>().HasData(
                new Circle { CircleId = circles[0], Name = "Inner Circle" },
                new Circle { CircleId = circles[1], Name = "Computational Social Science Lab" },
                new Circle { CircleId = circles[2], Name = "Capital University Alumni" },
                new Circle { CircleId = circles[3], Name = "Proceedit Team" },
                new Circle { CircleId = circles[4], Name = "Tokyo GCI Cohort" },
                new Circle { CircleId = circles[5], Name = "Docker & k8s Devs" },
                new Circle { CircleId = circles[6], Name = "Institutional Economics Book Club" },
                new Circle { CircleId = circles[7], Name = "UC Berkeley Audit Group" },
                new Circle { CircleId = circles[8], Name = "Historic Cairo Explorers" },
                new Circle { CircleId = circles[9], Name = "C# Mentorship" }
            );

            modelBuilder.Entity<SocialMediaAccount>().HasData(
                new SocialMediaAccount { SocialMediaAccountId = smAccounts[0], Platform = "LinkedIn", Url = "https://linkedin.com/in/ned-ibrahim" },
                new SocialMediaAccount { SocialMediaAccountId = smAccounts[1], Platform = "Twitter", Url = "https://twitter.com/acemoglu" },
                new SocialMediaAccount { SocialMediaAccountId = smAccounts[2], Platform = "GitHub", Url = "https://github.com/munger-economics" },
                new SocialMediaAccount { SocialMediaAccountId = smAccounts[3], Platform = "ResearchGate", Url = "https://researchgate.net/profile/ann-swidler" },
                new SocialMediaAccount { SocialMediaAccountId = smAccounts[4], Platform = "Wikipedia", Url = "https://en.wikipedia.org/wiki/Emile_Durkheim" },
                new SocialMediaAccount { SocialMediaAccountId = smAccounts[5], Platform = "NobelPrize", Url = "https://nobelprize.org/douglass-north" },
                new SocialMediaAccount { SocialMediaAccountId = smAccounts[6], Platform = "LinkedIn", Url = "https://linkedin.com/in/youssef-dev" },
                new SocialMediaAccount { SocialMediaAccountId = smAccounts[7], Platform = "GitHub", Url = "https://github.com/kenji-gci" },
                new SocialMediaAccount { SocialMediaAccountId = smAccounts[8], Platform = "Instagram", Url = "https://instagram.com/sarah_cairo" },
                new SocialMediaAccount { SocialMediaAccountId = smAccounts[9], Platform = "Twitter", Url = "https://twitter.com/omar_tech" }
            );

            // ==========================================
            // 5. CORE BUSINESS SEED DATA (Persons)
            // ==========================================
            modelBuilder.Entity<Person>().HasData(
                new Person
                {
                    PersonId = persons[0],
                    Name = "Ned Ibrahim",
                    Gender = "Male",
                    phone = "+61411234567",
                    DateOfBirth = new DateTime(1998, 4, 12),
                    CountryId = countries[1],
                    NewsLetter = true,
                    email = "ned@example.com",
                    Origin = "Met online to discuss cultural differences",
                    ContextMemory = "Great conversations on social phenomena",
                    LinkedInProfile = "linkedin.com/in/ned-ibrahim",
                    Address = "Sydney, NSW",
                    ApplicationUserId = seedUserId
                },
                new Person
                {
                    PersonId = persons[1],
                    Name = "Daron Acemoglu",
                    Gender = "Male",
                    phone = "+12025550174",
                    DateOfBirth = new DateTime(1967, 9, 3),
                    CountryId = countries[2],
                    NewsLetter = false,
                    email = "daron@mit.edu",
                    Origin = "Institutional Economics research",
                    ContextMemory = "Author of Why Nations Fail",
                    LinkedInProfile = "linkedin.com/in/dacemoglu",
                    Address = "Cambridge, MA",
                    ApplicationUserId = seedUserId
                },
                new Person
                {
                    PersonId = persons[2],
                    Name = "Michael Munger",
                    Gender = "Male",
                    phone = "+19195550188",
                    DateOfBirth = new DateTime(1958, 9, 23),
                    CountryId = countries[2],
                    NewsLetter = false,
                    email = "munger@duke.edu",
                    Origin = "Academic lectures contact",
                    ContextMemory = "Excellent pedagogical style in lectures",
                    LinkedInProfile = "linkedin.com/in/mmunger",
                    Address = "Durham, NC",
                    ApplicationUserId = seedUserId
                },
                new Person
                {
                    PersonId = persons[3],
                    Name = "Ann Swidler",
                    Gender = "Female",
                    phone = "+15105550199",
                    DateOfBirth = new DateTime(1944, 12, 12),
                    CountryId = countries[2],
                    NewsLetter = true,
                    email = "swidler@berkeley.edu",
                    Origin = "UC Berkeley Audit",
                    ContextMemory = "Culture in Action sociology frameworks",
                    LinkedInProfile = "linkedin.com/in/aswidler",
                    Address = "Berkeley, CA",
                    ApplicationUserId = seedUserId
                },
                new Person
                {
                    PersonId = persons[4],
                    Name = "Emile Durkheim",
                    Gender = "Male",
                    phone = "+3315550123",
                    DateOfBirth = new DateTime(1858, 4, 15),
                    CountryId = countries[7],
                    NewsLetter = false,
                    email = "emile@sorbonne.fr",
                    Origin = "Sociology foundational reading",
                    ContextMemory = "Structural functionalism architect",
                    LinkedInProfile = "",
                    Address = "Paris, France",
                    ApplicationUserId = seedUserId
                },
                new Person
                {
                    PersonId = persons[5],
                    Name = "Douglass North",
                    Gender = "Male",
                    phone = "+13145550145",
                    DateOfBirth = new DateTime(1920, 11, 5),
                    CountryId = countries[2],
                    NewsLetter = false,
                    email = "north@wustl.edu",
                    Origin = "Institutional constraints research",
                    ContextMemory = "Nobel laureate in economics",
                    LinkedInProfile = "",
                    Address = "St. Louis, MO",
                    ApplicationUserId = seedUserId
                },
                new Person
                {
                    PersonId = persons[6],
                    Name = "Youssef",
                    Gender = "Male",
                    phone = "+201012345678",
                    DateOfBirth = new DateTime(2005, 8, 20),
                    CountryId = countries[0],
                    NewsLetter = true,
                    email = "youssef@capital.edu.eg",
                    Origin = "Capital University BIS",
                    ContextMemory = "Classmate in BIS academic program",
                    LinkedInProfile = "linkedin.com/in/youssef-bis",
                    Address = "Cairo, Egypt",
                    ApplicationUserId = seedUserId
                },
                new Person
                {
                    PersonId = persons[7],
                    Name = "Kenji",
                    Gender = "Male",
                    phone = "+81312345678",
                    DateOfBirth = new DateTime(2002, 1, 15),
                    CountryId = countries[3],
                    NewsLetter = true,
                    email = "kenji@tokyo.ac.jp",
                    Origin = "Tokyo GCI Cohort",
                    ContextMemory = "Data Science program partner",
                    LinkedInProfile = "linkedin.com/in/kenji-data",
                    Address = "Tokyo, Japan",
                    ApplicationUserId = seedUserId
                },
                new Person
                {
                    PersonId = persons[8],
                    Name = "Sarah",
                    Gender = "Female",
                    phone = "+201112345678",
                    DateOfBirth = new DateTime(2006, 5, 10),
                    CountryId = countries[0],
                    NewsLetter = false,
                    email = "sarah@proceedit.com",
                    Origin = "Proceedit Internship",
                    ContextMemory = "Software Engineering intern colleague",
                    LinkedInProfile = "linkedin.com/in/sarah-dev",
                    Address = "Cairo, Egypt",
                    ApplicationUserId = seedUserId
                },
                new Person
                {
                    PersonId = persons[9],
                    Name = "Omar",
                    Gender = "Male",
                    phone = "+201212345678",
                    DateOfBirth = new DateTime(2001, 7, 22),
                    CountryId = countries[0],
                    NewsLetter = true,
                    email = "omar@cairohistory.org",
                    Origin = "Historic Cairo Explorers",
                    ContextMemory = "Met at Beit Yakan architectural tour",
                    LinkedInProfile = "linkedin.com/in/omar-arch",
                    Address = "Giza, Egypt",
                    ApplicationUserId = seedUserId
                }
            );

            // ==========================================
            // 6. ONE-TO-MANY SEED DATA (ContactItemRole, Note, Interaction)
            // ==========================================
            modelBuilder.Entity<ContactItemRole>().HasData(
                new ContactItemRole { ContactsRoleId = roles[0], PersonId = persons[0] },
                new ContactItemRole { ContactsRoleId = roles[1], PersonId = persons[1] },
                new ContactItemRole { ContactsRoleId = roles[2], PersonId = persons[2] },
                new ContactItemRole { ContactsRoleId = roles[3], PersonId = persons[3] },
                new ContactItemRole { ContactsRoleId = roles[4], PersonId = persons[4] },
                new ContactItemRole { ContactsRoleId = roles[5], PersonId = persons[5] },
                new ContactItemRole { ContactsRoleId = roles[6], PersonId = persons[6] },
                new ContactItemRole { ContactsRoleId = roles[7], PersonId = persons[7] },
                new ContactItemRole { ContactsRoleId = roles[8], PersonId = persons[8] },
                new ContactItemRole { ContactsRoleId = roles[9], PersonId = persons[9] }
            );

            modelBuilder.Entity<Note>().HasData(
                new Note { NoteId = notes[0], NoteType = EnNoteType.Important, Content = "Discussed the framework of Douglass North regarding institutional constraints.", PersonId = persons[5] },
                new Note { NoteId = notes[1], NoteType = EnNoteType.Moderate, Content = "Finalized the scope for the paper: 'Structural Waste within Absorptive Structures'.", PersonId = persons[1] },
                new Note { NoteId = notes[2], NoteType = EnNoteType.Moderate, Content = "Troubleshooting the CI/CD pipeline. SonarCloud and Trivy are failing on the new C# build.", PersonId = persons[6] },
                new Note { NoteId = notes[3], NoteType = EnNoteType.Low, Content = "Need to schedule a visit to Beit Yakan and tour the Abdeen architectural sites.", PersonId = persons[8] },
                new Note { NoteId = notes[4], NoteType = EnNoteType.Important, Content = "Configured StatefulSets for the SQL Server database in Kubernetes.", PersonId = persons[7] },
                new Note { NoteId = notes[5], NoteType = EnNoteType.Moderate, Content = "Drafted the thank you email to Professor Ann Swidler for her sociology lectures.", PersonId = persons[3] },
                new Note { NoteId = notes[6], NoteType = EnNoteType.Important, Content = "Reviewing C# Repository Pattern and Serilog implementation for the Stock Management App.", PersonId = persons[9] },
                new Note { NoteId = notes[7], NoteType = EnNoteType.Moderate, Content = "Comparing cultural differences and social phenomena over a call.", PersonId = persons[0] },
                new Note { NoteId = notes[8], NoteType = EnNoteType.Low, Content = "Need to catch up on the latest UC Berkeley lecture notes.", PersonId = persons[4] },
                new Note { NoteId = notes[9], NoteType = EnNoteType.Important, Content = "Exploring the intersection of technical programming and CSS.", PersonId = persons[2] }
            );

            modelBuilder.Entity<Interaction>().HasData(
                new Interaction { InteractionId = interactions[0], InteractionTitle = "Research sync on structural waste", TimeOfInteraction = new DateTime(2026, 6, 1), PersonId = persons[5] },
                new Interaction { InteractionId = interactions[1], InteractionTitle = "Code Review: C# Web API", TimeOfInteraction = new DateTime(2026, 6, 2), PersonId = persons[9] },
                new Interaction { InteractionId = interactions[2], InteractionTitle = "Sociology debate on Durkheim", TimeOfInteraction = new DateTime(2026, 6, 3), PersonId = persons[3] },
                new Interaction { InteractionId = interactions[3], InteractionTitle = "Kubernetes cluster troubleshooting", TimeOfInteraction = new DateTime(2026, 6, 4), PersonId = persons[4] },
                new Interaction { InteractionId = interactions[4], InteractionTitle = "Cairo urban history walk planning", TimeOfInteraction = new DateTime(2026, 6, 5), PersonId = persons[8] },
                new Interaction { InteractionId = interactions[5], InteractionTitle = "GCI World Data Science kickoff", TimeOfInteraction = new DateTime(2026, 6, 6), PersonId = persons[7] },
                new Interaction { InteractionId = interactions[6], InteractionTitle = "GitHub Actions pairing session", TimeOfInteraction = new DateTime(2026, 6, 7), PersonId = persons[6] },
                new Interaction { InteractionId = interactions[7], InteractionTitle = "Catchup call across time zones", TimeOfInteraction = new DateTime(2026, 6, 8), PersonId = persons[0] },
                new Interaction { InteractionId = interactions[8], InteractionTitle = "Acemoglu reading discussion", TimeOfInteraction = new DateTime(2026, 6, 9), PersonId = persons[1] },
                new Interaction { InteractionId = interactions[9], InteractionTitle = "Institutional Economics framework mapping", TimeOfInteraction = new DateTime(2026, 6, 10), PersonId = persons[2] }
            );

            // ==========================================
            // 7. REMAINING JUNCTION TABLE MAPPINGS (still M:N)
            // ==========================================

            modelBuilder.Entity<Person>().HasMany(p => p.UserDefinedTags).WithMany(t => t.People).UsingEntity(j => j.HasData(
                new { PeoplePersonId = persons[0], UserDefinedTagsTagId = tags[9] },
                new { PeoplePersonId = persons[1], UserDefinedTagsTagId = tags[1] },
                new { PeoplePersonId = persons[2], UserDefinedTagsTagId = tags[1] },
                new { PeoplePersonId = persons[3], UserDefinedTagsTagId = tags[6] },
                new { PeoplePersonId = persons[4], UserDefinedTagsTagId = tags[6] },
                new { PeoplePersonId = persons[5], UserDefinedTagsTagId = tags[1] },
                new { PeoplePersonId = persons[6], UserDefinedTagsTagId = tags[0] },
                new { PeoplePersonId = persons[7], UserDefinedTagsTagId = tags[5] },
                new { PeoplePersonId = persons[8], UserDefinedTagsTagId = tags[2] },
                new { PeoplePersonId = persons[9], UserDefinedTagsTagId = tags[4] }
            ));

            modelBuilder.Entity<Person>().HasMany(p => p.SystemStatusTags).WithMany(t => t.People).UsingEntity(j => j.HasData(
                new { PeoplePersonId = persons[0], SystemStatusTagsStatusTagId = EnSystemStatusTag.ContactedRecently },
                new { PeoplePersonId = persons[1], SystemStatusTagsStatusTagId = EnSystemStatusTag.ShouldReply },
                new { PeoplePersonId = persons[2], SystemStatusTagsStatusTagId = EnSystemStatusTag.FollowUp },
                new { PeoplePersonId = persons[3], SystemStatusTagsStatusTagId = EnSystemStatusTag.HighPriority },
                new { PeoplePersonId = persons[4], SystemStatusTagsStatusTagId = EnSystemStatusTag.Ignored },
                new { PeoplePersonId = persons[5], SystemStatusTagsStatusTagId = EnSystemStatusTag.LowPriority },
                new { PeoplePersonId = persons[6], SystemStatusTagsStatusTagId = EnSystemStatusTag.ModeratePriority },
                new { PeoplePersonId = persons[7], SystemStatusTagsStatusTagId = EnSystemStatusTag.Urgent },
                new { PeoplePersonId = persons[8], SystemStatusTagsStatusTagId = EnSystemStatusTag.AlreadyReplied },
                new { PeoplePersonId = persons[9], SystemStatusTagsStatusTagId = EnSystemStatusTag.HaveNotReplied }
            ));

            modelBuilder.Entity<Person>().HasMany(p => p.Circles).WithMany(c => c.People).UsingEntity(j => j.HasData(
                new { PeoplePersonId = persons[0], CirclesCircleId = circles[0] },
                new { PeoplePersonId = persons[1], CirclesCircleId = circles[6] },
                new { PeoplePersonId = persons[2], CirclesCircleId = circles[6] },
                new { PeoplePersonId = persons[3], CirclesCircleId = circles[7] },
                new { PeoplePersonId = persons[4], CirclesCircleId = circles[7] },
                new { PeoplePersonId = persons[5], CirclesCircleId = circles[6] },
                new { PeoplePersonId = persons[6], CirclesCircleId = circles[9] },
                new { PeoplePersonId = persons[7], CirclesCircleId = circles[4] },
                new { PeoplePersonId = persons[8], CirclesCircleId = circles[2] },
                new { PeoplePersonId = persons[9], CirclesCircleId = circles[3] }
            ));

            // Channel memberships are user data — no seed rows. (An older
            // implicit-join seed was dropped with the join payload migration.)

            modelBuilder.Entity<SocialMediaAccount>().HasMany(s => s.People).WithMany(p => p.OtherSocialMediaAccounts).UsingEntity(j => j.HasData(
                new { PeoplePersonId = persons[0], OtherSocialMediaAccountsSocialMediaAccountId = smAccounts[0] },
                new { PeoplePersonId = persons[1], OtherSocialMediaAccountsSocialMediaAccountId = smAccounts[1] },
                new { PeoplePersonId = persons[2], OtherSocialMediaAccountsSocialMediaAccountId = smAccounts[2] },
                new { PeoplePersonId = persons[3], OtherSocialMediaAccountsSocialMediaAccountId = smAccounts[3] },
                new { PeoplePersonId = persons[4], OtherSocialMediaAccountsSocialMediaAccountId = smAccounts[4] },
                new { PeoplePersonId = persons[5], OtherSocialMediaAccountsSocialMediaAccountId = smAccounts[5] },
                new { PeoplePersonId = persons[6], OtherSocialMediaAccountsSocialMediaAccountId = smAccounts[6] },
                new { PeoplePersonId = persons[7], OtherSocialMediaAccountsSocialMediaAccountId = smAccounts[7] },
                new { PeoplePersonId = persons[8], OtherSocialMediaAccountsSocialMediaAccountId = smAccounts[8] },
                new { PeoplePersonId = persons[9], OtherSocialMediaAccountsSocialMediaAccountId = smAccounts[9] }
            ));
        }
    }
}