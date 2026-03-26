using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Departments
{
    public class UpdateDepartmentRequestDto : CreateDepartmentRequestDto 
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }
    }
}
