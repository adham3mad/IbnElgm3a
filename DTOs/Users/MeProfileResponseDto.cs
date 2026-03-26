using System;
using System.Text.Json.Serialization;
using IbnElgm3a.DTOs.Common;

namespace IbnElgm3a.DTOs.Users
{
    public class MeProfileResponseDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("full_name")]
        public string FullName { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("role")]
        public string Role { get; set; } = "student";

        [JsonPropertyName("avatar_url")]
        public string? AvatarUrl { get; set; }

        [JsonPropertyName("faculty")]
        public IdNameDto? Faculty { get; set; }

        [JsonPropertyName("department")]
        public IdNameDto? Department { get; set; }

        [JsonPropertyName("last_login")]
        public DateTimeOffset LastLogin { get; set; }
    }
}
