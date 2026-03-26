using IbnElgm3a.Enums;
using System;
using System.Text.Json.Serialization;
using IbnElgm3a.DTOs.Common;

namespace IbnElgm3a.DTOs.Complaints
{
    public class ComplaintListResponseDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("ticket_number")]
        public string TicketNumber { get; set; } = string.Empty;

        [JsonPropertyName("student")]
        public StudentSummaryDto? Student { get; set; }

        [JsonPropertyName("type")]
        public ComplaintType Type { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public ComplaintStatus Status { get; set; }

        [JsonPropertyName("is_overdue")]
        public bool IsOverdue { get; set; }

        [JsonPropertyName("assigned_to")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IdNameDto? AssignedTo { get; set; }

        [JsonPropertyName("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonPropertyName("last_response_at")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTimeOffset? LastResponseAt { get; set; }
    }
}
