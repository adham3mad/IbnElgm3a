using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Complaints
{
    public class ComplaintDetailResponseDto : ComplaintListResponseDto
    {
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("internal_note")]
        public string? InternalNote { get; set; }

        [JsonPropertyName("response")]
        public string? Response { get; set; }

        [JsonPropertyName("history")]
        public List<ComplaintHistoryDto> History { get; set; } = new();
    }
}
