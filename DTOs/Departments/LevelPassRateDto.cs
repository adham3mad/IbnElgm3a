using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Departments
{
    public class LevelPassRateDto
    {
        [JsonPropertyName("level")]
        public int Level { get; set; }

        [JsonPropertyName("pass_rate_pct")]
        public double PassRatePct { get; set; }
    }
}
