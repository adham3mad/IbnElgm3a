using IbnElgm3a.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Academics.Enrollments
{
    public class CreateEnrollmentRequestDto
    {
        [Required]
        [JsonPropertyName("student_id")]
        public string StudentId { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("section_id")]
        public string SectionId { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Enrolled;
    }
}
