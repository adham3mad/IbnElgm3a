using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Academics.Grades
{
    public class UpdateGradeRequestDto
    {
        [JsonPropertyName("marks")]
        public decimal? Marks { get; set; }

        [JsonPropertyName("comments")]
        public string? Comments { get; set; }
    }
}
