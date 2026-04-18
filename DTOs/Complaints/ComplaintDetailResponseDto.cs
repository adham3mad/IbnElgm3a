using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Complaints
{
    public class ComplaintDetailResponseDto : ComplaintListResponseDto
    {
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("internal_notes")]
        public List<InternalNoteDto> InternalNotes { get; set; } = new();

        [JsonPropertyName("response")]
        public string? Response { get; set; }

        [JsonPropertyName("history")]
        public List<ComplaintHistoryDto> History { get; set; } = new();
    }

    public class InternalNoteDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("author_name")]
        public string AuthorName { get; set; } = string.Empty;

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("created_at")]
        public System.DateTimeOffset CreatedAt { get; set; }
    }
}
