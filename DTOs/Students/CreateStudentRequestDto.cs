using IbnElgm3a.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using IbnElgm3a.DTOs.Guardians;

namespace IbnElgm3a.DTOs.Students
{
    public class CreateStudentRequestDto
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

        [JsonPropertyName("year")]
        public int? Year { get; set; }

        [JsonPropertyName("phone")]
        public string? Phone { get; set; }

        [JsonPropertyName("send_welcome")]
        public bool SendWelcome { get; set; } = true;

        [Required]
        [JsonPropertyName("birth_date")]
        public DateTimeOffset BirthDate { get; set; }
        
        [Required]
        [JsonPropertyName("gender")]
        public Gender Gender { get; set; }
        
        [JsonPropertyName("nationality")]
        public string? Nationality { get; set; }

        [JsonPropertyName("guardians")]
        public List<CreateGuardianRequestDto> Guardians { get; set; } = new();
    }
}
