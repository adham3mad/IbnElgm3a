using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Settings
{
    public class UpdateMeRequestDto
    {
        [JsonPropertyName("full_name")]
        public string? FullName { get; set; }

        [JsonPropertyName("phone")]
        public string? Phone { get; set; }
    }
}
