using IbnElgm3a.DTOs.Users;
using IbnElgm3a.DTOs.Common;
using IbnElgm3a.Models;
using IbnElgm3a.Services;
using IbnElgm3a.Services.Localization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IbnElgm3a.Controllers.Common
{
    [ApiController]
    [Route("users/me")]
    [Authorize]
    public class UsersMeController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILocalizationService _localizer;
        private readonly IConfiguration _config;
        private readonly IFileStorageService _fileStorage;

        public UsersMeController(AppDbContext context, ILocalizationService localizer, IConfiguration config, IFileStorageService fileStorage)
        {
            _context = context;
            _localizer = localizer;
            _config = config;
            _fileStorage = fileStorage;
        }

        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var userId = GetUserId();

            // Single projection query - no Include, no tracking
            var user = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new
                {
                    u.Id,
                    u.Name,
                    u.Email,
                    u.Phone,
                    u.AvatarUrl,
                    u.CreatedAt,
                    u.Status,
                    RoleName = u.Role != null ? u.Role.Name : null,
                    FacultyName = u.Faculty != null ? u.Faculty.Name : null,
                    DepartmentName = u.Department != null ? u.Department.Name : null,
                    // Student data (null if not student)
                    StudentId = u.Student != null ? u.Student.Id : null,
                    AcademicNumber = u.Student != null ? u.Student.AcademicNumber : null,
                    Level = u.Student != null ? (int?)u.Student.Level : null,
                    GPA = u.Student != null ? (decimal?)u.Student.GPA : null,
                    // Instructor data (null if not instructor)
                    InstructorRank = u.Instructor != null ? u.Instructor.Rank : null,
                    InstructorOfficeHours = u.Instructor != null ? u.Instructor.OfficeHours : null,
                    InstructorBio = u.Instructor != null ? u.Instructor.Bio : null,
                })
                .FirstOrDefaultAsync();

            if (user == null) return Unauthorized();

            var roleName = user.RoleName?.ToLower();

            if (roleName == "student" && user.StudentId != null)
            {
                // Compute completed credits in a single SUM query
                var completedCredits = await _context.Enrollments
                    .AsNoTracking()
                    .Where(e => e.StudentId == user.StudentId
                        && e.Grade != null
                        && e.Grade.LetterGrade != IbnElgm3a.Enums.LetterGrade.F)
                    .SumAsync(e => e.Section!.Course!.CreditHours);

                return Ok(new
                {
                    id = user.Id,
                    student_id = user.AcademicNumber ?? "",
                    name = user.Name,
                    email = user.Email,
                    phone = user.Phone,
                    faculty = user.FacultyName ?? "",
                    department = user.DepartmentName ?? "",
                    year = user.Level ?? 0,
                    enrollment_status = user.Status.ToString().ToLower(),
                    gpa = user.GPA ?? 0,
                    credit_hours_completed = completedCredits,
                    credit_hours_total = 132,
                    avatar_url = user.AvatarUrl,
                    notification_preferences = new
                    {
                        email = true,
                        push = true,
                        sms = false
                    },
                    created_at = user.CreatedAt
                });
            }
            else if (roleName == "instructor")
            {
                var nameParts = user.Name.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                var firstName = nameParts.Length > 0 ? nameParts[0] : "";
                var lastName = nameParts.Length > 1 ? nameParts[1] : "";
                var initials = (firstName.Length > 0 ? firstName[0].ToString() : "")
                             + (lastName.Length > 0 ? lastName[0].ToString() : "");

                return Ok(new
                {
                    data = new
                    {
                        id = user.Id,
                        first_name = firstName,
                        last_name = lastName,
                        full_name = user.Name,
                        phone = user.Phone,
                        title = user.InstructorRank ?? "",
                        department = user.DepartmentName ?? "",
                        email = user.Email,
                        avatar_url = user.AvatarUrl,
                        initials = initials.ToUpper(),
                        office_hours = user.InstructorOfficeHours,
                        bio = user.InstructorBio
                    }
                });
            }
            else
            {
                return Ok(new
                {
                    id = user.Id,
                    name = user.Name,
                    email = user.Email,
                    phone = user.Phone,
                    role = user.RoleName ?? "staff",
                    faculty = user.FacultyName ?? "",
                    department = user.DepartmentName ?? "",
                    avatar_url = user.AvatarUrl,
                    created_at = user.CreatedAt
                });
            }
        }

        [HttpPatch]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateMeProfileRequestDto request)
        {
            var userId = GetUserId();
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return Unauthorized();

            if (!string.IsNullOrEmpty(request.FullName)) user.Name = request.FullName;
            if (!string.IsNullOrEmpty(request.Email)) user.Email = request.Email;
            if (!string.IsNullOrEmpty(request.Phone))
            {
                var phonePattern = @"^(\+20|0020|0)?1[0125][0-9]{8}$";
                if (!System.Text.RegularExpressions.Regex.IsMatch(request.Phone, phonePattern))
                {
                    return BadRequest(Models.ApiResponse<object>.CreateError("INVALID_PHONE", _localizer.GetMessage("INVALID_PHONE")));
                }
                user.Phone = request.Phone;
            }

            await _context.SaveChangesAsync();

            return await GetProfile();
        }

        [HttpPatch("password")]
        public async Task<IActionResult> ChangePassword([FromBody] IbnElgm3a.DTOs.Auth.ChangePasswordRequestDto request)
        {
            var userId = GetUserId();
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return Unauthorized();

            var pepper = _config["PASSWORD_PEPPER"] ?? "";
            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword + pepper, user.PasswordHash))
            {
                return BadRequest(Models.ApiResponse<object>.CreateError("INVALID_PASSWORD", _localizer.GetMessage("INVALID_PASSWORD")));
            }

            if (request.NewPassword == request.CurrentPassword)
            {
                return BadRequest(Models.ApiResponse<object>.CreateError("SAME_PASSWORD_NOT_ALLOWED", _localizer.GetMessage("SAME_PASSWORD_NOT_ALLOWED")));
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword + pepper);
            user.MustChangePw = false;
            await _context.SaveChangesAsync();

            return Ok(new { message = _localizer.GetMessage("PASSWORD_CHANGED_SUCCESS") });
        }

        [HttpPost("avatar")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(Models.ApiResponse<object>.CreateError("FILE_EMPTY", _localizer.GetMessage("FILE_EMPTY")));

            if (file.Length > 2 * 1024 * 1024)
                return BadRequest(Models.ApiResponse<object>.CreateError("FILE_TOO_LARGE", _localizer.GetMessage("FILE_TOO_LARGE")));

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var extension = System.IO.Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(extension))
                return BadRequest(Models.ApiResponse<object>.CreateError("FILE_INVALID_TYPE", _localizer.GetMessage("FILE_INVALID_TYPE")));

            var userId = GetUserId();
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return Unauthorized();

            try
            {
                if (!string.IsNullOrEmpty(user.AvatarUrl) && user.AvatarUrl.Contains("/uploads/avatars/"))
                {
                    await _fileStorage.DeleteFileAsync(user.AvatarUrl, "uploads/avatars");
                }

                var fileUrl = await _fileStorage.SaveFileAsync(file, "uploads/avatars");
                user.AvatarUrl = fileUrl;
                await _context.SaveChangesAsync();

                return Ok(new { avatar_url = fileUrl });
            }
            catch (System.Exception)
            {
                return StatusCode(500, Models.ApiResponse<object>.CreateError("UPLOAD_FAILED", "An error occurred during file upload."));
            }
        }
    }
}
