using System.ComponentModel.DataAnnotations;

namespace ContactsManger.Core.DTOs
{
    public class RefreshRequest
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
