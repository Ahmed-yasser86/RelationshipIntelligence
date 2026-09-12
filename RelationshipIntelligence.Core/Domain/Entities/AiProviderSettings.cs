using System;
using System.ComponentModel.DataAnnotations;

namespace Entities
{
    /// <summary>
    /// Per-user AI provider configuration. The user chooses provider preset,
    /// model, base URL, and API key. The key is stored DataProtection-protected
    /// and is never returned by any API.
    /// </summary>
    public class AiProviderSettings
    {
        [Key]
        public Guid ApplicationUserId { get; set; }

        [Required]
        [StringLength(50)]
        public string Provider { get; set; } = "Custom";

        [Required]
        [StringLength(200)]
        public string Model { get; set; } = string.Empty;

        [StringLength(500)]
        public string? BaseUrl { get; set; }

        public string? ProtectedApiKey { get; set; }

        public DateTime UpdatedAtUtc { get; set; }
    }
}
