using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Dashboard
{
    public class DashboardStatsDto
    {
        [JsonPropertyName("total_students")]
        public int TotalStudents { get; set; }

        [JsonPropertyName("total_instructors")]
        public int TotalInstructors { get; set; }

        [JsonPropertyName("total_courses")]
        public int TotalCourses { get; set; }

        [JsonPropertyName("open_complaints")]
        public int OpenComplaints { get; set; }

        [JsonPropertyName("pass_rate")]
        public decimal PassRate { get; set; }
    }

    public class FacultySummaryDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("student_count")]
        public int StudentCount { get; set; }

        [JsonPropertyName("dept_count")]
        public int DeptCount { get; set; }

        [JsonPropertyName("pass_rate")]
        public decimal PassRate { get; set; }

        [JsonPropertyName("alert_count")]
        public int AlertCount { get; set; }
    }
}
