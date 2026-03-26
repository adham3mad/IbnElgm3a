using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Settings
{
    public class SystemSettingsResponseDto
    {
        [JsonPropertyName("maintenance_mode")]
        public bool MaintenanceMode { get; set; }

        [JsonPropertyName("allow_registration")]
        public bool AllowRegistration { get; set; }

        [JsonPropertyName("academic_year")]
        public string AcademicYear { get; set; } = string.Empty;

        [JsonPropertyName("current_semester_id")]
        public string CurrentSemesterId { get; set; } = string.Empty;

        [JsonPropertyName("contact_email")]
        public string ContactEmail { get; set; } = string.Empty;

        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0.0";
    }
}
