using System;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Settings
{
    public class ActiveSemesterDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("start_date")]
        public DateTimeOffset StartDate { get; set; }

        [JsonPropertyName("end_date")]
        public DateTimeOffset EndDate { get; set; }
    }
}
