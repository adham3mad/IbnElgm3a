using IbnElgm3a.Enums;
using System;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Academics.Enrollments
{
    public class EnrollmentResponseDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("student_id")]
        public string StudentId { get; set; } = string.Empty;

        [JsonPropertyName("section_id")]
        public string SectionId { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public EnrollmentStatus Status { get; set; }

        [JsonPropertyName("enrolled_at")]
        public DateTimeOffset EnrolledAt { get; set; }
    }
}
