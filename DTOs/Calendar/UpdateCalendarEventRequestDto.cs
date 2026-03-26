using IbnElgm3a.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Calendar
{
    public class UpdateCalendarEventRequestDto
    {
        [MaxLength(120)]
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("date")]
        public DateTimeOffset? Date { get; set; }

        [JsonPropertyName("end_date")]
        public DateTimeOffset? EndDate { get; set; }

        [JsonPropertyName("type")]
        public CalendarEventType? Type { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("semester_id")]
        public string? SemesterId { get; set; }

        [JsonPropertyName("is_public")]
        public bool? IsPublic { get; set; }
    }
}
