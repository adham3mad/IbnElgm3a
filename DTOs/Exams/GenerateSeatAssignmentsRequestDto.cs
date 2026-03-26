using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Exams
{
    public class GenerateSeatAssignmentsRequestDto
    {
        [JsonPropertyName("strategy")]
        public string Strategy { get; set; } = "alphabetical"; // alphabetical | random | by_gpa
    }
}
