using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Settings
{
    public class CreditHourRulesDto
    {
        [JsonPropertyName("max_per_semester")]
        public int MaxPerSemester { get; set; }

        [JsonPropertyName("min_for_graduation")]
        public int MinForGraduation { get; set; }
    }
}
