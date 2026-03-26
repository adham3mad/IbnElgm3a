using System;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Courses
{
    public class CourseSectionDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("instructor_name")]
        public string InstructorName { get; set; } = string.Empty;

        [JsonPropertyName("room_name")]
        public string RoomName { get; set; } = string.Empty;

        [JsonPropertyName("day")]
        public DayOfWeek Day { get; set; }

        [JsonPropertyName("start_time")]
        public string StartTime { get; set; } = string.Empty;

        [JsonPropertyName("capacity")]
        public int Capacity { get; set; }

        [JsonPropertyName("enrolled")]
        public int Enrolled { get; set; }
    }
}
