using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Auth
{
    public class ForgotPasswordRequestDto
    {
        [Required]
        [EmailAddress]
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;
        [Required]
        [JsonPropertyName("national_id")]
        public string NationalId { get; set; } = string.Empty;

        [JsonPropertyName("channel")]
        public string Channel { get; set; } = "email";
    }

    public class VerifyOtpRequestDto
    {
        [Required]
        [JsonPropertyName("national_id")]
        public string NationalId { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("otp_token")]
        public string OtpToken { get; set; } = string.Empty;

        [Required]
        [StringLength(6, MinimumLength = 6)]
        [JsonPropertyName("otp_code")]
        public string OtpCode { get; set; } = string.Empty;
    }

    public class ResetPasswordRequestDto
    {
        [Required]
        [JsonPropertyName("reset_token")]
        public string ResetToken { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        [StrongPassword]
        [JsonPropertyName("new_password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [Compare("NewPassword")]
        [JsonPropertyName("confirm_password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class ChangePasswordRequestDto
    {
        [Required]
        [JsonPropertyName("current_password")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        [StrongPassword]
        [JsonPropertyName("new_password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [Compare("NewPassword")]
        [JsonPropertyName("confirm_password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
