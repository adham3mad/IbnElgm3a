using IbnElgm3a.Enums;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Academics.Grades
{
    public class GradeResponseDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("enrollment_id")]
        public string EnrollmentId { get; set; } = string.Empty;

        [JsonPropertyName("marks")]
        public decimal Marks { get; set; }

        [JsonPropertyName("letter_grade")]
        public LetterGrade? LetterGrade { get; set; }

        [JsonPropertyName("comments")]
        public string? Comments { get; set; }
    }
}
