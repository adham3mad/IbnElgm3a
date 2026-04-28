using IbnElgm3a.DTOs.Users;
using IbnElgm3a.DTOs.Common;
using IbnElgm3a.Enums;
using IbnElgm3a.Models;
using IbnElgm3a.Models.Data;
using IbnElgm3a.Models;
using IbnElgm3a.Filters;
using IbnElgm3a.Services.Localization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IbnElgm3a.Controllers.Admins
{
    [ApiController]
    [Route("v1/admin/users")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly ILocalizationService _localizer;
        private readonly Microsoft.Extensions.DependencyInjection.IServiceScopeFactory _scopeFactory;

        public UsersController(AppDbContext context, IConfiguration config, ILocalizationService localizer, Microsoft.Extensions.DependencyInjection.IServiceScopeFactory scopeFactory)
        {
            _context = context;
            _config = config;
            _localizer = localizer;
            _scopeFactory = scopeFactory;
        }

        private async Task<string> GenerateAcademicNumberAsync(string facultyId, int year)
        {
            var faculty = await _context.Faculties.FindAsync(facultyId);
            if (faculty == null) throw new Exception(_localizer.GetMessage("FACULTY_NOT_FOUND"));
            if (string.IsNullOrEmpty(faculty.Code)) throw new Exception(_localizer.GetMessage("INTERNAL_FACULTY_CODE_MISSING"));

            var yearPart = year.ToString().Substring(year.ToString().Length - 2);
            var facCode = faculty.Code.PadRight(4, '0').Substring(0, 4);
            var prefix = yearPart + facCode;
            
            var lastNumber = await _context.Students
                .Where(s => s.AcademicNumber.StartsWith(prefix))
                .OrderByDescending(s => s.AcademicNumber)
                .Select(s => s.AcademicNumber)
                .FirstOrDefaultAsync();

            int nextSeq = 1;
            if (lastNumber != null && lastNumber.Length >= 12)
            {
                if (int.TryParse(lastNumber.Substring(6), out var currentSeq))
                {
                    nextSeq = currentSeq + 1;
                }
            }

            return prefix + nextSeq.ToString("D6");
        }
        
        [HttpGet]
        [RequirePermission(PermissionEnum.Dashboard_Users_Read)]
        public async Task<IActionResult> GetUsers(
            [FromQuery] UserRole role,
            [FromQuery] UserStatus? status = null,
            [FromQuery] string? q = null,
            [FromQuery] string? faculty_id = null,
            [FromQuery] string? dept_id = null,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 20,
            [FromQuery] string sort_by = "name",
            [FromQuery] string sort_dir = "asc")
        {
            var query = _context.Users
                .Include(u => u.Faculty)
                .Include(u => u.Department)
                .Include(u => u.Student)
                .Where(u => u.Role != null && u.Role.Name.ToLower() == role.ToString().ToLower());

            if (status.HasValue) query = query.Where(u => u.Status == status.Value);
            if (!string.IsNullOrEmpty(faculty_id)) query = query.Where(u => u.FacultyId == faculty_id);
            if (!string.IsNullOrEmpty(dept_id)) query = query.Where(u => u.DepartmentId == dept_id);
            if (!string.IsNullOrEmpty(q))
            {
                var qLower = q.ToLower();
                query = query.Where(u => u.Name.ToLower().Contains(qLower) || u.Email.ToLower().Contains(qLower));
            }

            var total = await query.CountAsync();
            var users = await query
                .OrderBy(u => u.Name) 
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(u => new UserListResponseDto
                {
                    Id = u.Id,
                    FullName = u.Name,
                    Email = u.Email,
                    StudentId = u.Student != null ? u.Student.AcademicNumber : null,
                    Faculty = u.Faculty != null ? new IdNameDto { Id = u.Faculty.Id, Name = u.Faculty.NameAr ?? u.Faculty.Name } : null,
                    Department = u.Department != null ? new IdNameDto { Id = u.Department.Id, Name = u.Department.NameAr ?? u.Department.Name } : null,
                    Year = u.Student != null ? u.Student.Level : null,
                    Gpa = u.Student != null ? u.Student.GPA : null,
                    Status = u.Status,
                    EnrolledAt = u.Student != null ? u.Student.EnrollmentDate : null
                }).ToListAsync();

            var pagination = new ApiPagination { Page = page, Limit = limit, Total = total, HasMore = (page * limit) < total };
            return Ok(ApiResponse<List<UserListResponseDto>>.CreateSuccess(users, pagination: pagination));
        }

        [HttpGet("{user_id}")]
        [RequirePermission(PermissionEnum.Dashboard_Users_Read)]
        public async Task<IActionResult> GetUserById(string user_id)
        {
            var user = await _context.Users
                .Include(u => u.Faculty)
                .Include(u => u.Department)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == user_id);

            if (user == null) return NotFound(ApiResponse<object>.CreateError("USER_NOT_FOUND", _localizer.GetMessage("USER_NOT_FOUND")));

            var dto = new UserListResponseDto
            {
                Id = user.Id,
                FullName = user.Name,
                Email = user.Email,
                Faculty = user.Faculty != null ? new IdNameDto { Id = user.Faculty.Id, Name = user.Faculty.NameAr ?? user.Faculty.Name } : null,
                Department = user.Department != null ? new IdNameDto { Id = user.Department.Id, Name = user.Department.NameAr ?? user.Department.Name } : null,
                Status = user.Status
            };

            return Ok(ApiResponse<UserListResponseDto>.CreateSuccess(dto));
        }

        [HttpPatch("{user_id}")]
        [RequirePermission(PermissionEnum.Dashboard_Users_Update)]
        public async Task<IActionResult> UpdateUser(string user_id, [FromBody] UpdateUserRequestDto request)
        {
            var user = await _context.Users.FindAsync(user_id);
            if (user == null) return NotFound(ApiResponse<object>.CreateError("USER_NOT_FOUND", _localizer.GetMessage("USER_NOT_FOUND")));

            if (!string.IsNullOrEmpty(request.FullName)) user.Name = request.FullName;
            if (!string.IsNullOrEmpty(request.Email)) user.Email = request.Email;
            if (!string.IsNullOrEmpty(request.NationalId)) user.NationalId = request.NationalId;
            if (!string.IsNullOrEmpty(request.Phone)) user.Phone = request.Phone;
            if (!string.IsNullOrEmpty(request.FacultyId)) user.FacultyId = request.FacultyId;
            if (!string.IsNullOrEmpty(request.DepartmentId)) user.DepartmentId = request.DepartmentId;
            if (request.Status.HasValue) user.Status = request.Status.Value;

            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.CreateSuccess(new { message = _localizer.GetMessage("USER_UPDATED") }));
        }

        [HttpPatch("{user_id}/status")]
        [RequirePermission(PermissionEnum.Dashboard_Users_UpdateStatus)]
        public async Task<IActionResult> UpdateUserStatus(string user_id, [FromBody] UpdateUserStatusRequestDto request)
        {
            var user = await _context.Users.FindAsync(user_id);
            if (user == null) return NotFound(ApiResponse<object>.CreateError("USER_NOT_FOUND", _localizer.GetMessage("USER_NOT_FOUND")));

            if (request.Status.HasValue) user.Status = request.Status.Value;
            user.InactiveReason = request.Reason ?? user.InactiveReason;
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.CreateSuccess(new { message = _localizer.GetMessage("USER_UPDATED") }));
        }

        [HttpDelete("{user_id}")]
        [RequirePermission(PermissionEnum.Dashboard_Users_Delete)]
        public async Task<IActionResult> DeleteUser(string user_id)
        {
            var user = await _context.Users.FindAsync(user_id);
            if (user == null) return NotFound(ApiResponse<object>.CreateError("USER_NOT_FOUND", _localizer.GetMessage("USER_NOT_FOUND")));

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.CreateSuccess(new { message = _localizer.GetMessage("DELETED_SUCCESS") }));
        }

        [HttpPost("import")]
        [RequirePermission(PermissionEnum.Dashboard_Users_Import)]
        public async Task<IActionResult> BulkImport([FromBody] ImportUsersRequestDto request)
        {
            var job = new BulkImportJob 
            { 
                Id = "job_" + Guid.NewGuid().ToString("N").Substring(0, 10), 
                Type = request.Type,
                FileUrl = request.FileUrl,
                Status = "pending" 
            };
            
            _context.BulkImportJobs.Add(job);
            await _context.SaveChangesAsync();

            // Fire and forget background task
            _ = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var currentJob = await db.BulkImportJobs.FindAsync(job.Id);
                if (currentJob == null) return;

                currentJob.Status = "processing";
                await db.SaveChangesAsync();

                try 
                {
                    // Simulated processing time
                    await Task.Delay(5000); 
                    
                    currentJob.Status = "done";
                    currentJob.Imported = 150; // Mocked
                    currentJob.Total = 150;
                }
                catch (Exception)
                {
                    currentJob.Status = "failed";
                }
                
                await db.SaveChangesAsync();
            });

            return Accepted(new { job_id = job.Id, status = job.Status });
        }

        [HttpGet("import/status/{job_id}")]
        [RequirePermission(PermissionEnum.Dashboard_Users_Import)]
        public async Task<IActionResult> GetBulkImportStatus(string job_id)
        {
            var job = await _context.BulkImportJobs.FindAsync(job_id);
            if (job == null) return NotFound(ApiResponse<object>.CreateError("JOB_NOT_FOUND", _localizer.GetMessage("JOB_NOT_FOUND")));

            return Ok(new 
            { 
                job_id = job.Id,
                status = job.Status, 
                total = job.Total, 
                imported = job.Imported,
                failed_rows = System.Text.Json.JsonSerializer.Deserialize<List<object>>(job.FailedRowsJson)
            });
        }
    }
}
