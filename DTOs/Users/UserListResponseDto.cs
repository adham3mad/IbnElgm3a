using IbnElgm3a.Enums;
using System;
using System.Text.Json.Serialization;
using IbnElgm3a.DTOs.Common;
using IbnElgm3a.DTOs.Students;
using IbnElgm3a.DTOs.Instructors;

namespace IbnElgm3a.DTOs.Users
{
    public class UserListResponseDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("full_name")]
        public string FullName { get; set; } = string.Empty;

        [JsonPropertyName("student_id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? StudentId { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("faculty")]
        public IdNameDto? Faculty { get; set; }

        [JsonPropertyName("department")]
        public IdNameDto? Department { get; set; }

        [JsonPropertyName("year")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Year { get; set; }

        [JsonPropertyName("gpa")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public decimal? Gpa { get; set; }

        [JsonPropertyName("status")]
        public UserStatus Status { get; set; }

        [JsonPropertyName("avatar_url")]
        public string? AvatarUrl { get; set; }

        [JsonPropertyName("enrolled_at")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTimeOffset? EnrolledAt { get; set; }

        [JsonPropertyName("student_data")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public StudentDetailsDto? StudentData { get; set; }

        [JsonPropertyName("instructor_data")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public InstructorDetailsDto? InstructorData { get; set; }
    }
}
