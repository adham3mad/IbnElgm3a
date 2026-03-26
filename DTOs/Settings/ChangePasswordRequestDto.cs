using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Settings
{
    public class ChangePasswordRequestDto
    {
        [Required]
        [JsonPropertyName("current_password")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [StrongPassword]
        [JsonPropertyName("new_password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [Compare("NewPassword")]
        [JsonPropertyName("confirm_password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
