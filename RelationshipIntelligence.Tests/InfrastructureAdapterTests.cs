using Entities;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RelationshipIntelligence.Infrastructure.Services;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CRUDTests
{
    public class InfrastructureAdapterTests
    {
        [Fact]
        public void FixedUserService_ExposesTheSuppliedOwnerId()
        {
            var ownerId = Guid.NewGuid();

            var service = new FixedUserService(ownerId);

            service.UserId.Should().Be(ownerId);
        }

        [Fact]
        public void ApiKeyProtector_RoundTripsAProtectedKey()
        {
            var directory = Path.Combine(Path.GetTempPath(), "ri-protector-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var provider = DataProtectionProvider.Create(new DirectoryInfo(directory));
                var protector = new ApiKeyProtector(provider);

                var protectedBytes = protector.Protect(Encoding.UTF8.GetBytes("super-secret-key"));
                var plain = protector.Unprotect(protectedBytes);

                Encoding.UTF8.GetString(plain).Should().Be("super-secret-key");
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        [Fact]
        public async Task FileEmailSender_WritesTheDigestEmailToConfiguredDirectory()
        {
            var directory = Path.Combine(Path.GetTempPath(), "ri-email-" + Guid.NewGuid().ToString("N"));
            try
            {
                var sender = new FileEmailSender(directory, NullLogger<FileEmailSender>.Instance);

                await sender.SendAsync("owner@example.com", "Digest", "<p>hello</p>", "hello");

                var files = Directory.GetFiles(directory, "*.html");
                files.Should().ContainSingle();
                (await File.ReadAllTextAsync(files[0])).Should().Contain("hello");
            }
            finally
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, recursive: true);
            }
        }

        [Fact]
        public void MaintenanceScoringFactory_CreatesOwnerScopedInfrastructure()
        {
            using var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();
            var options = new DbContextOptionsBuilder<AppDBContext>()
                .UseSqlite(connection)
                .Options;
            var factory = new MaintenanceScoringFactory(options, NullLoggerFactory.Instance);
            var ownerId = Guid.NewGuid();

            using (var enumerationContext = factory.CreateEnumerationContext())
            {
                enumerationContext.Should().NotBeNull();
            }

            var pair = factory.CreateForOwner(ownerId);
            using (pair.Context)
            {
                pair.Scoring.Should().NotBeNull();
            }
        }
    }
}
