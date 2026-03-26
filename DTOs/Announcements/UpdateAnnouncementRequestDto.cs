using IbnElgm3a.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Announcements
{
    public class UpdateAnnouncementRequestDto
    {
        [MaxLength(120)]
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("body")]
        public string? Body { get; set; }

        [JsonPropertyName("target_type")]
        public AnnouncementTargetType? TargetType { get; set; }

        [JsonPropertyName("target_id")]
        public string? TargetId { get; set; }

        [JsonPropertyName("target_role")]
        public string? TargetRole { get; set; }

        [JsonPropertyName("priority")]
        public AnnouncementPriority? Priority { get; set; }
    }
}
