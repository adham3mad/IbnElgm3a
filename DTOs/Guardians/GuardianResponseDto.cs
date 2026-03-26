using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Guardians
{
    public class GuardianResponseDto : CreateGuardianRequestDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }
}
