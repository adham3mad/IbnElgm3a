using IbnElgm3a.Enums;
using System.Text.Json.Serialization;
using IbnElgm3a.DTOs.Common;
using IbnElgm3a.DTOs.Rooms;

namespace IbnElgm3a.DTOs.Schedules
{
    public class ScheduleSlotResponseDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("course")]
        public IdNameDto? Course { get; set; }

        [JsonPropertyName("section")]
        public IdNameDto? Section { get; set; }

        [JsonPropertyName("instructor")]
        public IdNameDto? Instructor { get; set; }

        [JsonPropertyName("room")]
        public RoomResponseDto? Room { get; set; }

        [JsonPropertyName("day")]
        public DayOfWeekEnum Day { get; set; }

        [JsonPropertyName("start_time")]
        public string StartTime { get; set; } = string.Empty;

        [JsonPropertyName("end_time")]
        public string EndTime { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public ClassType Type { get; set; }

        [JsonPropertyName("conflict")]
        public bool Conflict { get; set; }

        [JsonPropertyName("conflict_with")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ConflictWith { get; set; }
    }
}
