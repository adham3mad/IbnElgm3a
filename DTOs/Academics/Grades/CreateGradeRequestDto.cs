using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Academics.Grades
{
    public class CreateGradeRequestDto
    {
        [Required]
        [JsonPropertyName("enrollment_id")]
        public string EnrollmentId { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("marks")]
        public decimal Marks { get; set; }

        [JsonPropertyName("comments")]
        public string? Comments { get; set; }
    }
}
