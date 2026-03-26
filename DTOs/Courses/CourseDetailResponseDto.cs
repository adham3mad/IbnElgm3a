using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using IbnElgm3a.DTOs.Common;

namespace IbnElgm3a.DTOs.Courses
{
    public class CourseDetailResponseDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("name_ar")]
        public string? NameAr { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("credit_hours")]
        public int CreditHours { get; set; }

        [JsonPropertyName("department")]
        public IdNameDto? Department { get; set; }

        [JsonPropertyName("prerequisites")]
        public List<IdNameDto> Prerequisites { get; set; } = new();

        [JsonPropertyName("syllabus")]
        public string? Syllabus { get; set; }

        [JsonPropertyName("sections")]
        public List<CourseSectionDto> Sections { get; set; } = new();
    }
}
