using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Rooms
{
    public class CreateRoomRequestDto
    {
        [Required]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Range(1, 1000)]
        [JsonPropertyName("capacity")]
        public int Capacity { get; set; }
    }
}
