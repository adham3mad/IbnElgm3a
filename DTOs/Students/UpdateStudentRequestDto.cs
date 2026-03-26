using IbnElgm3a.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using IbnElgm3a.DTOs.Guardians;

namespace IbnElgm3a.DTOs.Students
{
    public class UpdateStudentRequestDto
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

        [JsonPropertyName("year")]
        public int? Year { get; set; }

        [JsonPropertyName("phone")]
        public string? Phone { get; set; }

        [JsonPropertyName("birth_date")]
        public DateTimeOffset? BirthDate { get; set; }
        
        [JsonPropertyName("gender")]
        public Gender? Gender { get; set; }
        
        [JsonPropertyName("nationality")]
        public string? Nationality { get; set; }

        [JsonPropertyName("guardians")]
        public List<CreateGuardianRequestDto>? Guardians { get; set; }
    }
}
