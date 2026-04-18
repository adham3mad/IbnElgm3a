using IbnElgm3a.Enums;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.SubAdmins
{
    public class UpdateSubAdminRequestDto
    {
        [JsonPropertyName("role_id")]
        public string? RoleId { get; set; }

        [JsonPropertyName("scope_type")]
        public SubAdminScopeType? ScopeType { get; set; }

        [JsonPropertyName("scope_id")]
        public string? ScopeId { get; set; }

        [JsonPropertyName("is_active")]
        public bool? IsActive { get; set; }

        [JsonPropertyName("permissions")]
        public List<string>? Permissions { get; set; }
    }
}
