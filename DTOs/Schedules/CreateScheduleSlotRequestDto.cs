using IbnElgm3a.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Schedules
{
    public class CreateScheduleSlotRequestDto
    {
        [Required]
        [JsonPropertyName("course_section_id")]
        public string CourseSectionId { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("room_id")]
        public string RoomId { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("day")]
        public DayOfWeekEnum Day { get; set; }

        [Required]
        [RegularExpression(@"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]$")]
        [JsonPropertyName("start_time")]
        public string StartTime { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]$")]
        [JsonPropertyName("end_time")]
        public string EndTime { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("type")]
        public ClassType Type { get; set; }

        [JsonPropertyName("recurrence")]
        public ScheduleRecurrence Recurrence { get; set; } = ScheduleRecurrence.Weekly;

        [Required]
        [JsonPropertyName("semester_id")]
        public string SemesterId { get; set; } = string.Empty;
    }
}
