using IbnElgm3a.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.SubAdmins
{
    public class CreateSubAdminRequestDto
    {
        [JsonPropertyName("user_id")]
        public string? UserId { get; set; } // Omit to create new

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("full_name")]
        public string? FullName { get; set; }

        [Required]
        [JsonPropertyName("scope_type")]
        public SubAdminScopeType ScopeType { get; set; }

        [JsonPropertyName("scope_id")]
        public string? ScopeId { get; set; }

        [Required]
        [JsonPropertyName("role_id")]
        public string RoleId { get; set; } = string.Empty;
    }
}
