using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Settings
{
    public class UpdateSettingsRequestDto
    {
        [JsonPropertyName("university.name")]
        public string? UniversityName { get; set; }

        [JsonPropertyName("university.name_ar")]
        public string? UniversityNameAr { get; set; }

        [JsonPropertyName("grading.scale")]
        public List<GradingScaleDto>? GradingScale { get; set; }

        [JsonPropertyName("security.password_min_len")]
        public int? PasswordMinLen { get; set; }

        [JsonPropertyName("security.max_login_attempts")]
        public int? MaxLoginAttempts { get; set; }

        [JsonPropertyName("security.session_timeout")]
        public int? SessionTimeout { get; set; }

        [JsonPropertyName("notifications.system_alerts")]
        public bool? SystemAlerts { get; set; }

        [JsonPropertyName("localization.default_lang")]
        public string? DefaultLang { get; set; }

        [JsonPropertyName("localization.timezone")]
        public string? Timezone { get; set; }
    }
}
