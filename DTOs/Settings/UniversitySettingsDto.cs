using System.Text.Json.Serialization;
using IbnElgm3a.DTOs.Academics.Semesters;

namespace IbnElgm3a.DTOs.Settings
{
    public class UniversitySettingsDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("name_ar")]
        public string? NameAr { get; set; }

        [JsonPropertyName("logo_url")]
        public string LogoUrl { get; set; } = string.Empty;

        [JsonPropertyName("active_semester")]
        public ActiveSemesterDto ActiveSemester { get; set; } = new ActiveSemesterDto();
    }
}
