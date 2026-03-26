using System.Text.Json.Serialization;
using IbnElgm3a.DTOs.Common;

namespace IbnElgm3a.DTOs.Courses
{
    public class CourseSummaryDto : IdNameDto
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;
    }
}
