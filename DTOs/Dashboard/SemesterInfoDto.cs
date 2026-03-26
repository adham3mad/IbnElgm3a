using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Dashboard
{
    public class SemesterInfoDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("current_week")]
        public int CurrentWeek { get; set; }

        [JsonPropertyName("total_weeks")]
        public int TotalWeeks { get; set; }

        [JsonPropertyName("next_event")]
        public string NextEvent { get; set; } = string.Empty;
    }
}
