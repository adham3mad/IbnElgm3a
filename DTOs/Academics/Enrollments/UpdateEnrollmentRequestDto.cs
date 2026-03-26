using IbnElgm3a.Enums;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Academics.Enrollments
{
    public class UpdateEnrollmentRequestDto
    {
        [JsonPropertyName("status")]
        public EnrollmentStatus? Status { get; set; }
    }
}
