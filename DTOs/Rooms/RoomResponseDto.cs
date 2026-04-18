using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Rooms
{
    public class RoomResponseDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("capacity")]
        public int Capacity { get; set; }

        [JsonPropertyName("type")]
        public IbnElgm3a.Enums.RoomType Type { get; set; }
    }
}
