namespace ServiceContracts.DTOs.CopilotDTOs
{
    public class AiProviderSettingsSaveRequest
    {
        public string? Provider { get; set; }

        public string? Model { get; set; }

        public string? BaseUrl { get; set; }

        /// <summary>
        /// Optional on save: when empty, the previously stored key is kept.
        /// Never returned by any read API.
        /// </summary>
        public string? ApiKey { get; set; }
    }
}
