using IbnElgm3a.Enums;
using System;
using System.Text.Json.Serialization;
using IbnElgm3a.DTOs.Common;

namespace IbnElgm3a.DTOs.Announcements
{
    public class AnnouncementListResponseDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("body")]
        public string Body { get; set; } = string.Empty;

        [JsonPropertyName("target_type")]
        public AnnouncementTargetType TargetType { get; set; }

        [JsonPropertyName("target_label")]
        public string TargetLabel { get; set; } = string.Empty;

        [JsonPropertyName("target_id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? TargetId { get; set; }

        [JsonPropertyName("sent_count")]
        public int SentCount { get; set; }

        [JsonPropertyName("read_count")]
        public int ReadCount { get; set; }

        [JsonPropertyName("created_by")]
        public IdNameDto? CreatedBy { get; set; }

        [JsonPropertyName("created_at")]
        public DateTimeOffset CreatedAt { get; set; }
    }
}
