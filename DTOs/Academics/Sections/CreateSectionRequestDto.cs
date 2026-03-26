using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Academics.Sections
{
    public class CreateSectionRequestDto
    {
        [Required]
        [JsonPropertyName("course_id")]
        public string CourseId { get; set; } = string.Empty;

        [JsonPropertyName("instructor_id")]
        public string? InstructorId { get; set; }

        [JsonPropertyName("room_id")]
        public string? RoomId { get; set; }

        [Required]
        [JsonPropertyName("capacity")]
        public int Capacity { get; set; }
    }
}
