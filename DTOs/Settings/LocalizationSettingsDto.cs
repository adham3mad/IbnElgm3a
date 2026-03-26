using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Settings
{
    public class LocalizationSettingsDto
    {
        [JsonPropertyName("default_lang")]
        public string DefaultLang { get; set; } = "en"; // ar | en

        [JsonPropertyName("timezone")]
        public string Timezone { get; set; } = "Africa/Cairo";
    }
}
