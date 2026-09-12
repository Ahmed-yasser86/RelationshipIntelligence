using System;

namespace ServiceContracts.DTOs.CopilotDTOs
{
    public class AiProviderSettingsResponse
    {
        public string Provider { get; set; } = "Custom";

        public string Model { get; set; } = string.Empty;

        public string? BaseUrl { get; set; }

        public bool HasKey { get; set; }

        public DateTime? UpdatedAtUtc { get; set; }
    }
}
