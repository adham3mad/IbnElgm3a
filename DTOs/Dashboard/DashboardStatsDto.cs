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
    }
}
