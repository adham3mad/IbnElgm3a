using IbnElgm3a.Enums;
using System;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Calendar
{
    public class CalendarEventResponseDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("date")]
        public DateTimeOffset Date { get; set; }

        [JsonPropertyName("end_date")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTimeOffset? EndDate { get; set; }

        [JsonPropertyName("type")]
        public CalendarEventType Type { get; set; }

        [JsonPropertyName("description")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Description { get; set; }

        [JsonPropertyName("is_public")]
        public bool IsPublic { get; set; }

        [JsonPropertyName("color_seed")]
        public string ColorSeed { get; set; } = string.Empty;
    }
}
