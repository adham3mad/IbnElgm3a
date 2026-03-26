using System;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Dashboard
{
    public class ActivityDto
    {
        [JsonPropertyName("icon")]
        public string Icon { get; set; } = string.Empty;

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("performed_at")]
        public DateTimeOffset PerformedAt { get; set; }

        [JsonPropertyName("actor_name")]
        public string ActorName { get; set; } = string.Empty;
    }
}
