using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Settings
{
    public class UpdateSystemSettingsRequestDto
    {
        [JsonPropertyName("maintenance_mode")]
        public bool? MaintenanceMode { get; set; }

        [JsonPropertyName("allow_registration")]
        public bool? AllowRegistration { get; set; }

        [JsonPropertyName("current_semester_id")]
        public string? CurrentSemesterId { get; set; }

        [JsonPropertyName("contact_email")]
        public string? ContactEmail { get; set; }
    }
}
