using System;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Faculties
{
    public class DeanDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("full_name")]
        public string FullName { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("appointed_since")]
        public DateTimeOffset? AppointedSince { get; set; }
    }
}
