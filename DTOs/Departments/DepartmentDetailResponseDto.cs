using System.Text.Json.Serialization;
using System.Collections.Generic;
using IbnElgm3a.DTOs.Common;
using IbnElgm3a.DTOs.Faculties;

namespace IbnElgm3a.DTOs.Departments
{
    public class DepartmentDetailResponseDto : DepartmentResponseDto
    {
        [JsonPropertyName("faculty")]
        public IdNameDto Faculty { get; set; } = new IdNameDto();

        [JsonPropertyName("level_count")]
        public int LevelCount { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = "active";

        [JsonPropertyName("head")]
        public DeanDto? Head { get; set; }

        [JsonPropertyName("pass_rate_by_level")]
        public List<LevelPassRateDto> PassRateByLevel { get; set; } = new List<LevelPassRateDto>();

        [JsonPropertyName("accent_color")]
        public string? AccentColor { get; set; }
    }
}
