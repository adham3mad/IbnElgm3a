using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Guardians
{
    public class UpdateGuardianRequestDto
    {
        [JsonPropertyName("full_name")]
        public string? FullName { get; set; }

        [JsonPropertyName("national_id")]
        public string? NationalId { get; set; }

        [JsonPropertyName("phone")]
        public string? Phone { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("address")]
        public string? Address { get; set; }

        [JsonPropertyName("job")]
        public string? Job { get; set; }
    }
}
