using System.Text.Json.Serialization;
using IbnElgm3a.DTOs.Common;

namespace IbnElgm3a.DTOs.Courses
{
    public class CourseListResponseDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("department")]
        public IdNameDto? Department { get; set; }

        [JsonPropertyName("credit_hours")]
        public int CreditHours { get; set; }

        [JsonPropertyName("enrolled_count")]
        public int EnrolledCount { get; set; }

        [JsonPropertyName("instructor")]
        public IdNameDto? Instructor { get; set; }

        [JsonPropertyName("section_count")]
        public int SectionCount { get; set; }
    }
}
