using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Common
{
    public class IdNameDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }
}
