using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Academics.Sections
{
    public class UpdateSectionRequestDto
    {
        [JsonPropertyName("instructor_id")]
        public string? InstructorId { get; set; }

        [JsonPropertyName("room_id")]
        public string? RoomId { get; set; }

        [JsonPropertyName("capacity")]
        public int? Capacity { get; set; }
    }
}
