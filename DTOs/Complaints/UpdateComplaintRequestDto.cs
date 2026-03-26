using IbnElgm3a.Enums;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Complaints
{
    public class UpdateComplaintRequestDto
    {
        [JsonPropertyName("status")]
        public ComplaintStatus? Status { get; set; }

        [JsonPropertyName("assigned_to")]
        public string? AssignedTo { get; set; }

        [JsonPropertyName("response")]
        public string? Response { get; set; }

        [JsonPropertyName("internal_note")]
        public string? InternalNote { get; set; }
    }
}
