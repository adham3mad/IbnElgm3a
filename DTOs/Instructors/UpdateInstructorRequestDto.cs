using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Instructors
{
    public class UpdateInstructorRequestDto
    {
        [JsonPropertyName("full_name")]
        public string? FullName { get; set; }

        [JsonPropertyName("full_name_ar")]
        public string? FullNameAr { get; set; }

        [EmailAddress]
        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [StringLength(14, MinimumLength = 14)]
        [JsonPropertyName("national_id")]
        public string? NationalId { get; set; }

        [JsonPropertyName("faculty_id")]
        public string? FacultyId { get; set; }

        [JsonPropertyName("department_id")]
        public string? DepartmentId { get; set; }

        [JsonPropertyName("phone")]
        public string? Phone { get; set; }

        [JsonPropertyName("rank")]
        public string? Rank { get; set; }

        [JsonPropertyName("office_hours")]
        public string? OfficeHours { get; set; }
    }
}
