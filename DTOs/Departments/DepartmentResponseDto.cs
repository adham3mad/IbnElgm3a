using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Departments
{
    public class DepartmentResponseDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("name_ar")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? NameAr { get; set; }

        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("student_count")]
        public int StudentCount { get; set; }

        [JsonPropertyName("instructor_count")]
        public int InstructorCount { get; set; }

        [JsonPropertyName("course_count")]
        public int CourseCount { get; set; }

        [JsonPropertyName("pass_rate_pct")]
        public double PassRatePct { get; set; }
    }
}
