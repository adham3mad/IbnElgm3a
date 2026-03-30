using IbnElgm3a.Enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Auth
{
    public class LoginRequestDto
    {
        [Required]
        [JsonPropertyName("national_id")]
        public string NationalId { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;

        [JsonPropertyName("remember_me")]
        public bool? RememberMe { get; set; }

        [JsonPropertyName("device_id")]
        public string? DeviceId { get; set; }

        [JsonPropertyName("fcm_token")]
        public string? FcmToken { get; set; }
    }

    public class RefreshRequestDto
    {
        [Required]
        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class LogoutRequestDto
    {
        [Required]
        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;

        [JsonPropertyName("all_devices")]
        public bool? AllDevices { get; set; }
    }
}
