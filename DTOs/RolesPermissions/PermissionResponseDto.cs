using IbnElgm3a.Enums;
using System;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.RolesPermissions
{
    public class PermissionResponseDto
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("name_ar")]
        public string? ArName { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("description_ar")]
        public string? ArDescription { get; set; }
    }
}
