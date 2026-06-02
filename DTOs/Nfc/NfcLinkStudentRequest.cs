using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Nfc
{
    public class NfcLinkStudentRequest
    {
        [Required]
        public string Uid { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("student_id")]
        public string StudentId { get; set; } = string.Empty;
    }
}
