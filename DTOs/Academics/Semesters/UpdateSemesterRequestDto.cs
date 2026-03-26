using System;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Academics.Semesters
{
    public class UpdateSemesterRequestDto
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("start_date")]
        public DateTimeOffset? StartDate { get; set; }

        [JsonPropertyName("end_date")]
        public DateTimeOffset? EndDate { get; set; }
    }
}
