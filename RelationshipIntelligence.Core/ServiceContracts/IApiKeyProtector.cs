namespace ServiceContracts
{
    /// <summary>
    /// Protects sensitive application data (currently AI provider API keys).
    /// The implementation and its cryptographic framework belong to an outer layer.
    /// </summary>
    public interface IApiKeyProtector
    {
        byte[] Protect(byte[] plaintext);
        byte[] Unprotect(byte[] protectedData);
    }
}
