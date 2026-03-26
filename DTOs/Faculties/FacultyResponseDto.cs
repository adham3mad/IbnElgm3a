using System.Text.Json.Serialization;
using IbnElgm3a.DTOs.Departments;
using IbnElgm3a.DTOs.Common;

namespace IbnElgm3a.DTOs.Faculties
{
    public class FacultyResponseDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("name_ar")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? NameAr { get; set; }

        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("student_count")]
        public int StudentCount { get; set; }

        [JsonPropertyName("head_of_faculty")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IdNameDto? HeadOfFaculty { get; set; }

        [JsonPropertyName("departments")]
        public List<DepartmentResponseDto> Departments { get; set; } = new List<DepartmentResponseDto>();
    }
}
