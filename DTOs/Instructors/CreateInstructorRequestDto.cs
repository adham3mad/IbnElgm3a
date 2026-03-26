using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Instructors
{
    public class CreateInstructorRequestDto
    {
        [Required]
        [MinLength(3)]
        [JsonPropertyName("full_name")]
        public string FullName { get; set; } = string.Empty;

        [JsonPropertyName("full_name_ar")]
        public string? FullNameAr { get; set; }

        [Required]
        [EmailAddress]
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(14, MinimumLength = 14)]
        [JsonPropertyName("national_id")]
        public string NationalId { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("faculty_id")]
        public string FacultyId { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("department_id")]
        public string DepartmentId { get; set; } = string.Empty;

        [JsonPropertyName("phone")]
        public string? Phone { get; set; }

        [JsonPropertyName("send_welcome")]
        public bool SendWelcome { get; set; } = true;

        [JsonPropertyName("rank")]
        public string? Rank { get; set; }

        [JsonPropertyName("office_hours")]
        public string? OfficeHours { get; set; }
    }
}
