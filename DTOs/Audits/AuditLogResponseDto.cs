using System;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Audits
{
    public class AuditLogResponseDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("user_id")]
        public string? UserId { get; set; }

        [JsonPropertyName("actor_name")]
        public string? ActorName { get; set; }

        [JsonPropertyName("action")]
        public string Action { get; set; } = string.Empty;

        [JsonPropertyName("entity")]
        public string Entity { get; set; } = string.Empty;

        [JsonPropertyName("entity_id")]
        public string? EntityId { get; set; }

        [JsonPropertyName("performed_at")]
        public DateTimeOffset PerformedAt { get; set; }

        [JsonPropertyName("details")]
        public string? Details { get; set; }

        [JsonPropertyName("ip_address")]
        public string? IpAddress { get; set; }

        [JsonPropertyName("icon")]
        public string? Icon { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
}
