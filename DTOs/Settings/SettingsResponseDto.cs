using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Settings
{
    public class SettingsResponseDto
    {
        [JsonPropertyName("university")]
        public UniversitySettingsDto University { get; set; } = new UniversitySettingsDto();

        [JsonPropertyName("grading")]
        public GradingSettingsDto Grading { get; set; } = new GradingSettingsDto();

        [JsonPropertyName("security")]
        public SecuritySettingsDto Security { get; set; } = new SecuritySettingsDto();

        [JsonPropertyName("notifications")]
        public NotificationSettingsDto Notifications { get; set; } = new NotificationSettingsDto();

        [JsonPropertyName("localization")]
        public LocalizationSettingsDto Localization { get; set; } = new LocalizationSettingsDto();
    }
}
