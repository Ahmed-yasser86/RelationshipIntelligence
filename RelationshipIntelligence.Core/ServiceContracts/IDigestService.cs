using Entities;
using ServiceContracts.DTOs;
using System.Threading.Tasks;

namespace ServiceContracts
{
    public interface IDigestService
    {
        Task<DigestPayload> BuildAsync(string baseUrl);

        Task<DigestEmailPreview> PreviewAsync(string baseUrl, string recipientEmail, string? customNote);

        Task<DigestPayload?> DeliverAsync(string baseUrl, string recipientEmail, string? customNote);

        Task<bool> HandleActionAsync(string? token, string action);

        Task<DigestPreference> GetPreferenceAsync();

        Task SetPreferenceAsync(bool enabled, double threshold, int count);
    }
}
