using IbnElgm3a.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.RolesPermissions
{
    public class RoleRequestDto
    {
        [Required]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("name_ar")]
        public string? NameAr { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [Required]
        [JsonPropertyName("type")]
        public AppType? Type { get; set; }

        [JsonPropertyName("permission_ids")]
        public List<Guid> PermissionIds { get; set; } = new List<Guid>();
    }
}
