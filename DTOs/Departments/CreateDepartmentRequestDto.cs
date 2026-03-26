using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Departments
{
    public class CreateDepartmentRequestDto
    {
        [Required]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("name_ar")]
        public string? NameAr { get; set; }

        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("dep_code")]
        public string? DepCode { get; set; }

        [JsonPropertyName("level_count")]
        public int LevelCount { get; set; } = 4;

        [JsonPropertyName("head_user_id")]
        public string? HeadUserId { get; set; }

        [JsonPropertyName("accent_color")]
        public string? AccentColor { get; set; }
    }
}
