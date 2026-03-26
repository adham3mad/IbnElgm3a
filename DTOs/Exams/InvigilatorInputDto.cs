using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Exams
{
    public class InvigilatorInputDto
    {
        [Required]
        [JsonPropertyName("user_id")]
        public string UserId { get; set; } = string.Empty;
    }
}
