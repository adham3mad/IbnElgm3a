using IbnElgm3a.Enums;
using System;
using System.Text.Json.Serialization;
using IbnElgm3a.DTOs.Courses;
using IbnElgm3a.DTOs.Rooms;

namespace IbnElgm3a.DTOs.Exams
{
    public class ExamListResponseDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("course")]
        public CourseSummaryDto? Course { get; set; }

        [JsonPropertyName("type")]
        public ExamType Type { get; set; }

        [JsonPropertyName("date")]
        public DateTimeOffset Date { get; set; }

        [JsonPropertyName("start_time")]
        public string StartTime { get; set; } = string.Empty;

        [JsonPropertyName("duration_minutes")]
        public int DurationMinutes { get; set; }

        [JsonPropertyName("hall")]
        public RoomResponseDto? Hall { get; set; }

        [JsonPropertyName("enrolled_count")]
        public int EnrolledCount { get; set; }

        [JsonPropertyName("status")]
        public ExamStatus Status { get; set; }

        [JsonPropertyName("has_seat_plan")]
        public bool HasSeatPlan { get; set; }
    }
}
