using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using IbnElgm3a.Enums;

namespace IbnElgm3a.DTOs.Rooms
{
    public class CreateRoomRequestDto
    {
        [Required]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [Required]
        [Range(1, 1000)]
        [JsonPropertyName("capacity")]
        public int Capacity { get; set; }

        [JsonPropertyName("type")]
        public RoomType? Type { get; set; }

        [JsonPropertyName("faculty_id")]
        public string? FacultyId { get; set; }
    }
}
