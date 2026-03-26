using IbnElgm3a.Enums;
using System;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.SubAdmins
{
    public class SubAdminListResponseDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("full_name")]
        public string FullName { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("scope")]
        public string Scope { get; set; } = string.Empty;

        [JsonPropertyName("scope_type")]
        public SubAdminScopeType ScopeType { get; set; }

        [JsonPropertyName("scope_id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ScopeId { get; set; }

        [JsonPropertyName("role_id")]
        public string RoleId { get; set; } = string.Empty;

        [JsonPropertyName("role_name")]
        public string RoleName { get; set; } = string.Empty;

        [JsonPropertyName("is_active")]
        public bool IsActive { get; set; }

        [JsonPropertyName("last_active_at")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTimeOffset? LastActiveAt { get; set; }
    }
}
