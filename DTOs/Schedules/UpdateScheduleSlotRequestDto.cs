using IbnElgm3a.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Schedules
{
    public class UpdateScheduleSlotRequestDto
    {
        [JsonPropertyName("course_section_id")]
        public string? CourseSectionId { get; set; }

        [JsonPropertyName("room_id")]
        public string? RoomId { get; set; }

        [JsonPropertyName("day")]
        public DayOfWeekEnum? Day { get; set; }

        [RegularExpression(@"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]$")]
        [JsonPropertyName("start_time")]
        public string? StartTime { get; set; }

        [RegularExpression(@"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]$")]
        [JsonPropertyName("end_time")]
        public string? EndTime { get; set; }

        [JsonPropertyName("type")]
        public ClassType? Type { get; set; }

        [JsonPropertyName("recurrence")]
        public ScheduleRecurrence? Recurrence { get; set; }

        [JsonPropertyName("semester_id")]
        public string? SemesterId { get; set; }
    }
}
