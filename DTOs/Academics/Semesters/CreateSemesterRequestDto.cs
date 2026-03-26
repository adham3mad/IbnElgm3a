using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Academics.Semesters
{
    public class CreateSemesterRequestDto
    {
        [Required]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("start_date")]
        public DateTimeOffset StartDate { get; set; }

        [Required]
        [JsonPropertyName("end_date")]
        public DateTimeOffset EndDate { get; set; }
    }
}
