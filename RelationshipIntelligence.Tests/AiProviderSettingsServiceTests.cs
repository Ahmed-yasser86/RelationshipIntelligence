using Entities;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Moq;
using RepositryContracts;
using ServiceContracts;
using ServiceContracts.DTOs.CopilotDTOs;
using Servicess;
using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Xunit;

namespace CRUDTests
{
    public class AiProviderSettingsServiceTests
    {
        private sealed class FakeProtector : IDataProtector
        {
            public IDataProtector CreateProtector(string purpose) => this;

            public byte[] Protect(byte[] plaintext) => System.Text.Encoding.UTF8.GetBytes(
                "PROTECTED:" + System.Text.Encoding.UTF8.GetString(plaintext));

            public byte[] Unprotect(byte[] protectedData)
            {
                var s = System.Text.Encoding.UTF8.GetString(protectedData);
                if (!s.StartsWith("PROTECTED:", StringComparison.Ordinal))
                    throw new CryptographicException("Bad payload.");
                return System.Text.Encoding.UTF8.GetBytes(s["PROTECTED:".Length..]);
            }
        }

        private sealed class FakeProtectionProvider : IDataProtectionProvider
        {
            public IDataProtector CreateProtector(string purpose) => new FakeProtector();
        }

        private readonly Guid _userA = Guid.NewGuid();
        private readonly Mock<AiProviderSettingsRepositoryContract> _repoMock = new();
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<ICurrentUserService> _userMock = new();

        private static string Stored(string secret) =>
            Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("PROTECTED:" + secret));

        private AiProviderSettingsService Service() => new(
            _repoMock.Object,
            _uowMock.Object,
            _userMock.Object,
            new FakeProtectionProvider(),
            Mock.Of<ILogger<AiProviderSettingsService>>());

        [Fact]
        public async Task GetAsync_NeverReturnsKey()
        {
            _userMock.Setup(u => u.UserId).Returns(_userA);
            _repoMock.Setup(r => r.GetAsync(_userA)).ReturnsAsync(new AiProviderSettings
            {
                ApplicationUserId = _userA,
                Provider = "Gemini",
                Model = "gemini-3.1-flash-lite",
                BaseUrl = "https://example.com",
                ProtectedApiKey = Stored("secret"),
                UpdatedAtUtc = DateTime.UtcNow
            });

            var result = await Service().GetAsync();

            result.Provider.Should().Be("Gemini");
            result.Model.Should().Be("gemini-3.1-flash-lite");
            result.HasKey.Should().BeTrue();
            result.Should().BeOfType<AiProviderSettingsResponse>();
        }

        [Fact]
        public async Task SaveAsync_InvalidProvider_Throws()
        {
            _userMock.Setup(u => u.UserId).Returns(_userA);

            await Assert.ThrowsAsync<ArgumentException>(() => Service().SaveAsync(new AiProviderSettingsSaveRequest
            {
                Provider = "Unknown",
                Model = "x"
            }));
            _uowMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task SaveAsync_BadUrl_Throws()
        {
            _userMock.Setup(u => u.UserId).Returns(_userA);

            await Assert.ThrowsAsync<ArgumentException>(() => Service().SaveAsync(new AiProviderSettingsSaveRequest
            {
                Provider = "Custom",
                Model = "m",
                BaseUrl = "not a url"
            }));
        }

        [Fact]
        public async Task SaveAsync_EmptyKey_KeepsExistingKey()
        {
            _userMock.Setup(u => u.UserId).Returns(_userA);
            _repoMock.Setup(r => r.GetAsync(_userA)).ReturnsAsync(new AiProviderSettings
            {
                ApplicationUserId = _userA,
                Provider = "Gemini",
                Model = "old",
                ProtectedApiKey = Stored("old-secret")
            });
            AiProviderSettings? saved = null;
            _repoMock.Setup(r => r.UpsertAsync(It.IsAny<AiProviderSettings>()))
                .Callback<AiProviderSettings>(s => saved = s)
                .Returns(Task.CompletedTask);

            await Service().SaveAsync(new AiProviderSettingsSaveRequest
            {
                Provider = "Gemini",
                Model = "gemini-3.1-flash-lite",
                ApiKey = "   "
            });

            saved.Should().NotBeNull();
            saved!.ProtectedApiKey.Should().Be(Stored("old-secret"));
            saved.Model.Should().Be("gemini-3.1-flash-lite");
        }

        [Fact]
        public async Task GetUnprotectedKeyAsync_RoundTrips()
        {
            _userMock.Setup(u => u.UserId).Returns(_userA);
            _repoMock.Setup(r => r.GetAsync(_userA)).ReturnsAsync(new AiProviderSettings
            {
                ApplicationUserId = _userA,
                Provider = "Gemini",
                Model = "m",
                ProtectedApiKey = Stored("secret")
            });

            (await Service().GetUnprotectedKeyAsync()).Should().Be("secret");
        }
    }
}
