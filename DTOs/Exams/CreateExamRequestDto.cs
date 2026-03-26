using IbnElgm3a.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Exams
{
    public class CreateExamRequestDto
    {
        [Required]
        [JsonPropertyName("course_id")]
        public string CourseId { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("semester_id")]
        public string SemesterId { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("type")]
        public ExamType Type { get; set; }

        [Required]
        [JsonPropertyName("date")]
        public DateTimeOffset Date { get; set; }

        [Required]
        [RegularExpression(@"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]$")]
        [JsonPropertyName("start_time")]
        public string StartTime { get; set; } = string.Empty;

        [Required]
        [Range(60, 240)]
        [JsonPropertyName("duration_minutes")]
        public int DurationMinutes { get; set; }

        [Required]
        [JsonPropertyName("hall_id")]
        public string HallId { get; set; } = string.Empty;

        [JsonPropertyName("invigilators")]
        public List<InvigilatorInputDto>? Invigilators { get; set; }
    }
}
