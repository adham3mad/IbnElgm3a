using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Users
{
    public class CreateAdminRequestDto
    {
        [Required]
        [MinLength(3)]
        [JsonPropertyName("full_name")]
        public string FullName { get; set; } = string.Empty;

        [JsonPropertyName("full_name_ar")]
        public string? FullNameAr { get; set; }

        [Required]
        [EmailAddress]
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(14, MinimumLength = 14)]
        [JsonPropertyName("national_id")]
        public string NationalId { get; set; } = string.Empty;

        [JsonPropertyName("phone")]
        public string? Phone { get; set; }

        [Required]
        [JsonPropertyName("role_id")]
        public string RoleId { get; set; } = string.Empty;
    }
}
