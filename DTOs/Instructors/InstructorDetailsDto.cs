using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Instructors
{
    public class InstructorDetailsDto
    {
        [JsonPropertyName("rank")]
        public string? Rank { get; set; }

        [JsonPropertyName("office_hours")]
        public string? OfficeHours { get; set; }
    }
}
