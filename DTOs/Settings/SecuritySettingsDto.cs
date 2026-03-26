using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Settings
{
    public class SecuritySettingsDto
    {
        [Range(6, 20)]
        [JsonPropertyName("password_min_len")]
        public int PasswordMinLen { get; set; }

        [Range(3, 10)]
        [JsonPropertyName("max_login_attempts")]
        public int MaxLoginAttempts { get; set; }

        [Range(5, 1440)]
        [JsonPropertyName("session_timeout")]
        public int SessionTimeout { get; set; }
    }
}
