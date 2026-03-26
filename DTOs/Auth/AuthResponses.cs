using IbnElgm3a.Enums;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Auth
{
    public class AuthTokensDto
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public DateTime ExpiresIn { get; set; }
    }

    public class AuthUserDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("role")]
        public UserRole Role { get; set; }

        [JsonPropertyName("full_name")]
        public string FullName { get; set; } = string.Empty;

        [JsonPropertyName("full_name_ar")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? FullNameAr { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("avatar_url")]
        public string? AvatarUrl { get; set; }

        [JsonPropertyName("faculty_id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? FacultyId { get; set; }

        [JsonPropertyName("department_id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? DepartmentId { get; set; }

        [JsonPropertyName("student_id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? StudentId { get; set; }

        [JsonPropertyName("permissions")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? Permissions { get; set; }

        [JsonPropertyName("must_change_pw")]
        public bool MustChangePw { get; set; }

        [JsonPropertyName("profile_complete")]
        public bool ProfileComplete { get; set; }
    }

    public class LoginResponseDto
    {
        [JsonPropertyName("tokens")]
        public AuthTokensDto Tokens { get; set; } = new AuthTokensDto();

        [JsonPropertyName("user")]
        public AuthUserDto User { get; set; } = new AuthUserDto();
    }
}
