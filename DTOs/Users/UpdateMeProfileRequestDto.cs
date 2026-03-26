using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Users
{
    public class UpdateMeProfileRequestDto
    {
        [JsonPropertyName("full_name")]
        public string? FullName { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("phone")]
        public string? Phone { get; set; }
    }
}
