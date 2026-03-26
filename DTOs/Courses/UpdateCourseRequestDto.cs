using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Courses
{
    public class UpdateCourseRequestDto
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("name_ar")]
        public string? NameAr { get; set; }

        [JsonPropertyName("department_id")]
        public string? DepartmentId { get; set; }

        [Range(1, 6)]
        [JsonPropertyName("credit_hours")]
        public int? CreditHours { get; set; }

        [JsonPropertyName("semester_id")]
        public string? SemesterId { get; set; }

        [JsonPropertyName("instructor_id")]
        public string? InstructorId { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("syllabus")]
        public string? Syllabus { get; set; }

        [JsonPropertyName("max_students")]
        public int? MaxStudents { get; set; }
    }
}
