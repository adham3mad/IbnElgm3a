using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Faculties
{
    public class FacultyDetailResponseDto : FacultyResponseDto
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = "active";

        [JsonPropertyName("established_year")]
        public int EstablishedYear { get; set; }

        [JsonPropertyName("building")]
        public string? Building { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("phone")]
        public string? Phone { get; set; }

        [JsonPropertyName("dean")]
        public DeanDto? Dean { get; set; }

        [JsonPropertyName("instructor_count")]
        public int InstructorCount { get; set; }

        [JsonPropertyName("department_count")]
        public int DepartmentCount { get; set; }

        [JsonPropertyName("pass_rate_pct")]
        public double PassRatePct { get; set; }

        [JsonPropertyName("active_courses_count")]
        public int ActiveCoursesCount { get; set; }

        [JsonPropertyName("graduates_this_year")]
        public int GraduatesThisYear { get; set; }

        [JsonPropertyName("new_admissions_count")]
        public int NewAdmissionsCount { get; set; }

        [JsonPropertyName("open_complaints_count")]
        public int OpenComplaintsCount { get; set; }

        [JsonPropertyName("settings")]
        public FacultySettingsDto Settings { get; set; } = new FacultySettingsDto();
    }
}
