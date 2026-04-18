using IbnElgm3a.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Exams
{
    public class UpdateExamRequestDto
    {
        [JsonPropertyName("course_id")]
        public string? CourseId { get; set; }

        [JsonPropertyName("semester_id")]
        public string? SemesterId { get; set; }

        [JsonPropertyName("type")]
        public ExamType? Type { get; set; }

        [JsonPropertyName("date")]
        public DateTimeOffset? Date { get; set; }

        [RegularExpression(@"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]$")]
        [JsonPropertyName("start_time")]
        public string? StartTime { get; set; }

        [Range(60, 240)]
        [JsonPropertyName("duration_minutes")]
        public int? DurationMinutes { get; set; }

        [JsonPropertyName("hall_id")]
        public string? HallId { get; set; }

        [JsonPropertyName("status")]
        public ExamStatus? Status { get; set; }

        [JsonPropertyName("seating_strategy")]
        public SeatingStrategy? SeatingStrategy { get; set; }
    }
}
