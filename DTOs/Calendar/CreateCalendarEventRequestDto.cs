using IbnElgm3a.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Calendar
{
    public class CreateCalendarEventRequestDto
    {
        [Required]
        [MaxLength(120)]
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("date")]
        public DateTimeOffset Date { get; set; }

        [JsonPropertyName("end_date")]
        public DateTimeOffset? EndDate { get; set; }

        [Required]
        [JsonPropertyName("type")]
        public CalendarEventType Type { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [Required]
        [JsonPropertyName("semester_id")]
        public string SemesterId { get; set; } = string.Empty;

        [JsonPropertyName("is_public")]
        public bool IsPublic { get; set; } = true;

        [JsonPropertyName("send_announcement")]
        public bool SendAnnouncement { get; set; } = false;
    }
}
