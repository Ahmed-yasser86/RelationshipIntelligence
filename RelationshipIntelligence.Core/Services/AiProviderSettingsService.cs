using Entities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using RepositryContracts;
using SerilogTimings;
using ServiceContracts;
using ServiceContracts.DTOs.CopilotDTOs;
using System;
using System.Threading.Tasks;

namespace Servicess
{
    public class AiProviderSettingsService : IAiProviderSettingsService
    {
        private const string ProtectorPurpose = "AiProviderKey.v1";

        private static readonly string[] KnownProviders = { "OpenAI", "Gemini", "Custom" };

        private readonly AiProviderSettingsRepositoryContract _settings;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IDataProtector _protector;
        private readonly ILogger<AiProviderSettingsService> _logger;

        public AiProviderSettingsService(
            AiProviderSettingsRepositoryContract settings,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser,
            IDataProtectionProvider protectionProvider,
            ILogger<AiProviderSettingsService> logger)
        {
            _settings = settings;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _protector = protectionProvider.CreateProtector(ProtectorPurpose);
            _logger = logger;
        }

        private Guid OwnerId()
        {
            var id = _currentUser.UserId;
            if (id == null || id == Guid.Empty)
                throw new UnauthorizedAccessException("Cannot manage AI provider settings without an authenticated user.");
            return id.Value;
        }

        public async Task<AiProviderSettingsResponse> GetAsync()
        {
            var ownerId = OwnerId();
            var settings = await _settings.GetAsync(ownerId);
            if (settings == null)
                return new AiProviderSettingsResponse();

            return new AiProviderSettingsResponse
            {
                Provider = settings.Provider,
                Model = settings.Model,
                BaseUrl = settings.BaseUrl,
                HasKey = !string.IsNullOrEmpty(settings.ProtectedApiKey),
                UpdatedAtUtc = settings.UpdatedAtUtc
            };
        }

        public async Task<AiProviderSettingsResponse> SaveAsync(AiProviderSettingsSaveRequest request)
        {
            using (Operation.Time("Save AI provider settings"))
            {
                if (request == null)
                    throw new ArgumentNullException(nameof(request));

                var provider = (request.Provider ?? "Custom").Trim();
                if (Array.Find(KnownProviders, p => string.Equals(p, provider, StringComparison.OrdinalIgnoreCase)) == null)
                    throw new ArgumentException("Provider must be OpenAI, Gemini, or Custom.", nameof(request.Provider));
                provider = char.ToUpperInvariant(provider[0]) + provider[1..].ToLowerInvariant();

                var model = (request.Model ?? string.Empty).Trim();
                if (model.Length == 0)
                    throw new ArgumentException("Model is required.", nameof(request.Model));
                if (model.Length > 200)
                    throw new ArgumentException("Model cannot exceed 200 characters.", nameof(request.Model));

                var baseUrl = string.IsNullOrWhiteSpace(request.BaseUrl) ? null : request.BaseUrl.Trim();
                if (baseUrl != null)
                {
                    if (baseUrl.Length > 500)
                        throw new ArgumentException("Base URL cannot exceed 500 characters.", nameof(request.BaseUrl));
                    if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri)
                        || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
                        throw new ArgumentException("Base URL must be a valid http(s) URL.", nameof(request.BaseUrl));
                }
                else
                {
                    baseUrl = DefaultBaseUrl(provider);
                }

                var ownerId = OwnerId();
                var existing = await _settings.GetAsync(ownerId);

                string? protectedKey = existing?.ProtectedApiKey;
                if (!string.IsNullOrWhiteSpace(request.ApiKey))
                {
                    var key = request.ApiKey.Trim();
                    if (key.Length > 500)
                        throw new ArgumentException("API key cannot exceed 500 characters.", nameof(request.ApiKey));
                    protectedKey = Convert.ToBase64String(
                        _protector.Protect(System.Text.Encoding.UTF8.GetBytes(key)));
                }

                await _settings.UpsertAsync(new AiProviderSettings
                {
                    ApplicationUserId = ownerId,
                    Provider = provider,
                    Model = model,
                    BaseUrl = baseUrl,
                    ProtectedApiKey = protectedKey,
                    UpdatedAtUtc = DateTime.UtcNow
                });
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Saved AI provider settings for owner {OwnerId} (provider {Provider}, model {Model})", ownerId, provider, model);
                return await GetAsync();
            }
        }

        public async Task<string?> GetUnprotectedKeyAsync()
        {
            var ownerId = OwnerId();
            var settings = await _settings.GetAsync(ownerId);
            if (settings == null || string.IsNullOrEmpty(settings.ProtectedApiKey))
                return null;

            try
            {
                var bytes = _protector.Unprotect(Convert.FromBase64String(settings.ProtectedApiKey));
                return System.Text.Encoding.UTF8.GetString(bytes);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Stored AI provider key could not be unprotected for owner {OwnerId}", ownerId);
                return null;
            }
        }

        private static string DefaultBaseUrl(string provider) => provider switch
        {
            "OpenAI" => "https://api.openai.com/v1",
            "Gemini" => "https://generativelanguage.googleapis.com/v1beta/openai",
            _ => "https://api.openai.com/v1"
        };
    }
}
