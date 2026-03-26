using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Faculties
{
    public class UpdateFacultyRequestDto : CreateFacultyRequestDto 
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }
    }
}
