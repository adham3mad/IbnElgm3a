using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Dashboard
{
    public class AlertDto
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("count")]
        public int Count { get; set; }
    }
}
