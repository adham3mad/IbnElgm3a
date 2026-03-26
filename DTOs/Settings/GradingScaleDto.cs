using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Settings
{
    public class GradingScaleDto
    {
        [JsonPropertyName("letter")]
        public string Letter { get; set; } = string.Empty;

        [JsonPropertyName("min_pct")]
        public int MinPct { get; set; }

        [JsonPropertyName("gpa_points")]
        public decimal GpaPoints { get; set; }
    }
}
