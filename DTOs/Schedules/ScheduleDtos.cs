using IbnElgm3a.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Schedules
{
    public class CreateSessionRequest
    {
        [Required]
        [JsonPropertyName("course_id")]
        public string CourseId { get; set; } = null!;

        [Required]
        [JsonPropertyName("type")]
        public ClassType Type { get; set; }

        [Required]
        [RegularExpression(@"^\d{4}-\d{2}-\d{2}$")]
        [JsonPropertyName("date")]
        public string Date { get; set; } = null!;

        [Required]
        [RegularExpression(@"^(?:[01]\d|2[0-3]):[0-5]\d$")]
        [JsonPropertyName("start_time")]
        public string StartTime { get; set; } = null!;

        [Required]
        [RegularExpression(@"^(?:[01]\d|2[0-3]):[0-5]\d$")]
        [JsonPropertyName("end_time")]
        public string EndTime { get; set; } = null!;

        [Required]
        [StringLength(100)]
        [JsonPropertyName("room")]
        public string Room { get; set; } = null!;

        [JsonPropertyName("is_recurring")]
        public bool IsRecurring { get; set; }

        [JsonPropertyName("send_reminder")]
        public bool SendReminder { get; set; }

        [JsonPropertyName("notes")]
        [StringLength(1000)]
        public string? Notes { get; set; }
    }

    public class UpdateSessionRequest
    {
        [JsonPropertyName("type")]
        public ClassType? Type { get; set; }

        [RegularExpression(@"^\d{4}-\d{2}-\d{2}$")]
        [JsonPropertyName("date")]
        public string? Date { get; set; }

        [RegularExpression(@"^(?:[01]\d|2[0-3]):[0-5]\d$")]
        [JsonPropertyName("start_time")]
        public string? StartTime { get; set; }

        [RegularExpression(@"^(?:[01]\d|2[0-3]):[0-5]\d$")]
        [JsonPropertyName("end_time")]
        public string? EndTime { get; set; }

        [StringLength(100)]
        [JsonPropertyName("room")]
        public string? Room { get; set; }

        [JsonPropertyName("notes")]
        [StringLength(1000)]
        public string? Notes { get; set; }
    }
}
