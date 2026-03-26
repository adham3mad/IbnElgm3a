using IbnElgm3a.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Users
{
    public class UpdateUserStatusRequestDto
    {
        [Required]
        [JsonPropertyName("status")]
        public UserStatus? Status { get; set; }

        [JsonPropertyName("reason")]
        public string? Reason { get; set; }
    }
}
