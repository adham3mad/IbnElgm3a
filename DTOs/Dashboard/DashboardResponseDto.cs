using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Dashboard
{
    public class DashboardResponseDto
    {
        [JsonPropertyName("stats")]
        public DashboardStatsDto Stats { get; set; } = new DashboardStatsDto();

        [JsonPropertyName("semester")]
        public SemesterInfoDto Semester { get; set; } = new SemesterInfoDto();

        [JsonPropertyName("alerts")]
        public List<AlertDto> Alerts { get; set; } = new List<AlertDto>();

        [JsonPropertyName("recent_activity")]
        public List<ActivityDto> RecentActivity { get; set; } = new List<ActivityDto>();

        [JsonPropertyName("module_badges")]
        public Dictionary<string, int> ModuleBadges { get; set; } = new Dictionary<string, int>();
    }
}
