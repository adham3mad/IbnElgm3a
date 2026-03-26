using IbnElgm3a.Enums;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.RolesPermissions
{
    public class FeatureResponseDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("name_ar")]
        public string? NameAr { get; set; }

        [JsonPropertyName("type")]
        public AppType Type { get; set; }

        [JsonPropertyName("permissions")]
        public List<PermissionResponseDto> Permissions { get; set; } = new List<PermissionResponseDto>();
    }
}
