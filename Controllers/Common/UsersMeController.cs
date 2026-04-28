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
    [Route("v1/users/me")]
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
            var user = await _context.Users
                .Include(u => u.Faculty)
                .Include(u => u.Department)
                .Include(u => u.Role)
                .Include(u => u.Student)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return Unauthorized();

            if (user.Role?.Name?.ToLower() == "student")
            {
                var student = user.Student;
                var completedCredits = await _context.Enrollments
                    .Include(e => e.Section)
                        .ThenInclude(s => s!.Course)
                    .Include(e => e.Grade)
                    .Where(e => e.StudentId == student!.Id && e.Grade != null && e.Grade.LetterGrade != IbnElgm3a.Enums.LetterGrade.F)
                    .SumAsync(e => e.Section!.Course!.CreditHours);

                return Ok(new
                {
                    id = user.Id,
                    student_id = student?.AcademicNumber ?? "",
                    name = user.Name,
                    email = user.Email,
                    phone = user.Phone,
                    faculty = user.Faculty?.Name ?? "",
                    department = user.Department?.Name ?? "",
                    year = student?.Level ?? 0,
                    enrollment_status = user.Status.ToString().ToLower(),
                    gpa = student?.GPA ?? 0,
                    credit_hours_completed = completedCredits,
                    credit_hours_total = 132, // Typical engineering total, could be dynamic later
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
            else
            {
                return Ok(new
                {
                    id = user.Id,
                    name = user.Name,
                    email = user.Email,
                    phone = user.Phone,
                    role = user.Role?.Name ?? "staff",
                    faculty = user.Faculty?.Name ?? "",
                    department = user.Department?.Name ?? "",
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
            if (!string.IsNullOrEmpty(request.Phone)) user.Phone = request.Phone;

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

            // Validation: Size (2MB)
            if (file.Length > 2 * 1024 * 1024)
                return BadRequest(Models.ApiResponse<object>.CreateError("FILE_TOO_LARGE", _localizer.GetMessage("FILE_TOO_LARGE")));

            // Validation: Type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var extension = System.IO.Path.GetExtension(file.FileName).ToLower();
            if (!System.Linq.Enumerable.Contains(allowedExtensions, extension))
                return BadRequest(Models.ApiResponse<object>.CreateError("FILE_INVALID_TYPE", _localizer.GetMessage("FILE_INVALID_TYPE")));

            var userId = GetUserId();
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return Unauthorized();

            try
            {
                // Delete old avatar if exists (optional but recommended)
                if (!string.IsNullOrEmpty(user.AvatarUrl) && user.AvatarUrl.Contains("/uploads/avatars/"))
                {
                    await _fileStorage.DeleteFileAsync(user.AvatarUrl, "uploads/avatars");
                }

                var fileUrl = await _fileStorage.SaveFileAsync(file, "uploads/avatars");
                user.AvatarUrl = fileUrl;
                await _context.SaveChangesAsync();

                return Ok(new { avatar_url = fileUrl });
            }
            catch (System.Exception ex)
            {
                // Log exception here in production
                return StatusCode(500, Models.ApiResponse<object>.CreateError("UPLOAD_FAILED", "An error occurred during file upload."));
            }
        }
    }
}
