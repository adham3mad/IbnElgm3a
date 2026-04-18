using System.Text.Json.Serialization;
using IbnElgm3a.Enums;

namespace IbnElgm3a.DTOs.Exams
{
    public class GenerateSeatAssignmentsRequestDto
    {
        [JsonPropertyName("strategy")]
        public SeatingStrategy? Strategy { get; set; }
    }
}
