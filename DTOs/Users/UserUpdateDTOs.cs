using IbnElgm3a.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Users
{
    public class UpdateUserRequestDto
    {
        [JsonPropertyName("full_name")]
        public string? FullName { get; set; }

        [JsonPropertyName("email")]
        [EmailAddress]
        public string? Email { get; set; }

        [JsonPropertyName("national_id")]
        public string? NationalId { get; set; }

        [JsonPropertyName("phone")]
        public string? Phone { get; set; }

        [JsonPropertyName("faculty_id")]
        public string? FacultyId { get; set; }

        [JsonPropertyName("dept_id")]
        public string? DepartmentId { get; set; }

        [JsonPropertyName("status")]
        public UserStatus? Status { get; set; }
    }

    public class ImportUsersRequestDto
    {
        [Required]
        [JsonPropertyName("file_url")]
        public string FileUrl { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("type")]
        public string Type { get; set; } = "students"; // students | instructors
    }
}
