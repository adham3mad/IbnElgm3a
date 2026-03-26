using System.Text.Json.Serialization;
using IbnElgm3a.DTOs.Common;

namespace IbnElgm3a.DTOs.Complaints
{
    public class StudentSummaryDto : IdNameDto
    {
        [JsonPropertyName("student_id")]
        public string StudentId { get; set; } = string.Empty;
    }
}
