using Microsoft.AspNetCore.DataProtection;
using ServiceContracts;

namespace RelationshipIntelligence.Infrastructure.Services
{
    public sealed class ApiKeyProtector : IApiKeyProtector
    {
        private const string ProtectorPurpose = "AiProviderKey.v1";

        private readonly IDataProtector _protector;

        public ApiKeyProtector(IDataProtectionProvider provider)
        {
            _protector = provider.CreateProtector(ProtectorPurpose);
        }

        public byte[] Protect(byte[] plaintext) => _protector.Protect(plaintext);

        public byte[] Unprotect(byte[] protectedData) => _protector.Unprotect(protectedData);
    }
}
