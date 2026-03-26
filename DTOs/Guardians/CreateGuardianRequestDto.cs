using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Guardians
{
    public class CreateGuardianRequestDto
    {
        [Required]
        [JsonPropertyName("full_name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(14, MinimumLength = 14)]
        [JsonPropertyName("national_id")]
        public string NationalId { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("phone")]
        public string Phone { get; set; } = string.Empty;

        [EmailAddress]
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("address")]
        public string Address { get; set; } = string.Empty;

        [JsonPropertyName("job")]
        public string Job { get; set; } = string.Empty;
    }
}
