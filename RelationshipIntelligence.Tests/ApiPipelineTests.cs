using Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Net.Http.Json;

namespace CRUDTests
{
    /// <summary>
    /// Exercises the real ASP.NET Core pipeline (startup, filters, routing,
    /// rate limiter and controllers) so the changed API plumbing is covered.
    /// The database is replaced with EF Core InMemory; no external services are called.
    /// </summary>
    public class ApiPipelineTests : IClassFixture<ApiPipelineTests.TestFactory>
    {
        private readonly TestFactory _factory;

        public ApiPipelineTests(TestFactory factory) => _factory = factory;

        [Theory]
        [InlineData("/api/Contacts/GetSystemStatusTags")]
        [InlineData("/api/Contacts/GetOrganizations")]
        [InlineData("/api/Contacts/GetAllCountries")]
        [InlineData("/api/Contacts/GetValidSearchFields")]
        [InlineData("/api/Contacts/GetValidCompositeFilterFields")]
        [InlineData("/api/Contacts/GetValidSortFields")]
        [InlineData("/api/Contacts/GetContactsGrid")]
        [InlineData("/api/Contacts/GetUpcomingEvents")]
        [InlineData("/api/Meeting/GetMeetings")]
        [InlineData("/api/Digest/GetDigestPreference")]
        [InlineData("/api/Digest/GetWeeklyDigest")]
        [InlineData("/api/Preference/GetGlobalDefaults")]
        [InlineData("/api/Preference/GetDueReminders")]
        [InlineData("/api/Network/GetNetworkGraph")]
        [InlineData("/api/Outreach/GetBatches")]
        [InlineData("/api/Ingestion/GetBatches")]
        [InlineData("/api/Ingestion/GetPending")]
        [InlineData("/api/Preference/GetPreference?personId=00000000-0000-0000-0000-000000000001")]
        [InlineData("/api/Meeting/GetMeeting?id=00000000-0000-0000-0000-000000000001")]
        [InlineData("/api/Digest/GetDigestPreview")]
        public async Task GetEndpoint_ExecutesPipelineAndReturnsClientOrSuccessStatus(string path)
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync(path);

            ((int)response.StatusCode).Should().BeLessThan(500,
                "the pipeline must handle the request without an unhandled server error");
        }

        [Fact]
        public async Task PostLogin_WithInvalidPayload_ReturnsClientError()
        {
            var client = _factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/Account/PostLogin",
                new { Email = "", Password = "" });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task PostRefresh_WithInvalidPayload_ReturnsClientError()
        {
            var client = _factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/Account/PostRefresh",
                new { Token = "", RefreshToken = "" });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        public sealed class TestFactory : WebApplicationFactory<Program>
        {
            static TestFactory()
            {
                Environment.SetEnvironmentVariable("JWT_KEY", "pipeline-test-signing-key-12345678901234567890");
                Environment.SetEnvironmentVariable("DIGEST_SECRET", "pipeline-test-digest-secret-12345678901234567890");
                Environment.SetEnvironmentVariable("ConnectionStrings__ContactDb",
                    "Server=(localdb)\\mssqllocaldb;Database=PipelineTests;Trusted_Connection=True");
                Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            }

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<DbContextOptions<AppDBContext>>();
                    services.AddDbContext<AppDBContext>(options =>
                        options.UseInMemoryDatabase("pipeline-tests"));
                    services.Configure<MvcOptions>(options => options.Filters.Clear());
                    services.AddAuthentication(TestAuthHandler.SchemeName)
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                            TestAuthHandler.SchemeName, _ => { });
                });
            }
        }

        private sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
        {
            public const string SchemeName = "PipelineTest";

            public TestAuthHandler(
                IOptionsMonitor<AuthenticationSchemeOptions> options,
                ILoggerFactory logger,
                UrlEncoder encoder)
                : base(options, logger, encoder) { }

            protected override Task<AuthenticateResult> HandleAuthenticateAsync()
            {
                var identity = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.Email, "pipeline@example.com")
                }, SchemeName);
                var principal = new ClaimsPrincipal(identity);
                return Task.FromResult(AuthenticateResult.Success(
                    new AuthenticationTicket(principal, SchemeName)));
            }
        }
    }
}
