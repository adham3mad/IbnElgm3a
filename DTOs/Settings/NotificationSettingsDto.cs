using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Settings
{
    public class NotificationSettingsDto
    {
        [JsonPropertyName("system_alerts")]
        public bool SystemAlerts { get; set; }
    }
}
