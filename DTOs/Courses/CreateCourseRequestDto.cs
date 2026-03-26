using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Courses
{
    public class CreateCourseRequestDto
    {
        [Required]
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("name_ar")]
        public string? NameAr { get; set; }

        [Required]
        [JsonPropertyName("department_id")]
        public string DepartmentId { get; set; } = string.Empty;

        [Required]
        [Range(1, 6)]
        [JsonPropertyName("credit_hours")]
        public int CreditHours { get; set; }

        [Required]
        [JsonPropertyName("semester_id")]
        public string SemesterId { get; set; } = string.Empty;

        [JsonPropertyName("instructor_id")]
        public string? InstructorId { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("max_students")]
        public int MaxStudents { get; set; } = 0;
    }
}
