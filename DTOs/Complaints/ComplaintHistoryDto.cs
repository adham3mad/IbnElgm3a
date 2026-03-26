using IbnElgm3a.Enums;
using System;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Complaints
{
    public class ComplaintHistoryDto
    {
        [JsonPropertyName("status")]
        public ComplaintStatus Status { get; set; }

        [JsonPropertyName("modified_by")]
        public string ModifiedBy { get; set; } = string.Empty;

        [JsonPropertyName("timestamp")]
        public DateTimeOffset Timestamp { get; set; }

        [JsonPropertyName("note")]
        public string? Note { get; set; }
    }
}
