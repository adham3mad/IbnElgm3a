using IbnElgm3a.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Announcements
{
    public class CreateAnnouncementRequestDto
    {
        [Required]
        [MaxLength(120)]
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("body")]
        public string Body { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("target_type")]
        public AnnouncementTargetType TargetType { get; set; }

        [JsonPropertyName("target_id")]
        public string? TargetId { get; set; }

        [JsonPropertyName("target_role")]
        public string? TargetRole { get; set; }

        [JsonPropertyName("send_push")]
        public bool SendPush { get; set; } = true;

        [JsonPropertyName("send_email")]
        public bool SendEmail { get; set; } = false;

        [JsonPropertyName("priority")]
        public AnnouncementPriority Priority { get; set; } = AnnouncementPriority.Normal;
    }
}
