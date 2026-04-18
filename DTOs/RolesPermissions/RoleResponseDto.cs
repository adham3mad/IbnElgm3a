using IbnElgm3a.Enums;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.RolesPermissions
{
    public class RoleResponseDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("name_ar")]
        public string? NameAr { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("type")]
        public AppType Type { get; set; }

        [JsonPropertyName("is_active")]
        public bool IsActive { get; set; }

        [JsonPropertyName("permissions")]
        public List<FeatureResponseDto> Permissions { get; set; } = new();
    }
}
