using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Academics.Sections
{
    public class SectionResponseDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("course_id")]
        public string CourseId { get; set; } = string.Empty;

        [JsonPropertyName("instructor_id")]
        public string? InstructorId { get; set; }

        [JsonPropertyName("room_id")]
        public string? RoomId { get; set; }

        [JsonPropertyName("capacity")]
        public int Capacity { get; set; }

        [JsonPropertyName("enrolled_count")]
        public int EnrolledCount { get; set; }
    }
}
