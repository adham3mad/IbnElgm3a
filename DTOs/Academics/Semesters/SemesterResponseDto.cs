using System;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Academics.Semesters
{
    public class SemesterResponseDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("start_date")]
        public DateTimeOffset StartDate { get; set; }

        [JsonPropertyName("end_date")]
        public DateTimeOffset EndDate { get; set; }

        [JsonPropertyName("is_active")]
        public bool IsActive { get; set; }

        [JsonPropertyName("total_weeks")]
        public int TotalWeeks { get; set; }

        [JsonPropertyName("current_week")]
        public int CurrentWeek { get; set; }

        [JsonPropertyName("next_event")]
        public string? NextEvent { get; set; }
    }
}
