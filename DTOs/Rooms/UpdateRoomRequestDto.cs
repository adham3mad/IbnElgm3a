using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Rooms
{
    public class UpdateRoomRequestDto
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [Range(1, 1000)]
        [JsonPropertyName("capacity")]
        public int? Capacity { get; set; }
    }
}
