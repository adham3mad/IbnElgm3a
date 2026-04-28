using IbnElgm3a.DTOs.Instructors;
using IbnElgm3a.DTOs.Common;
using IbnElgm3a.DTOs.Users;
using IbnElgm3a.Enums;
using IbnElgm3a.Models;
using IbnElgm3a.Models.Data;
using IbnElgm3a.Models;
using IbnElgm3a.Models.Data;
using IbnElgm3a.Filters;
using IbnElgm3a.Services.Localization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IbnElgm3a.Services;

namespace IbnElgm3a.Controllers.Admins
{
    [ApiController]
    [Route("v1/admin/instructors")]
    [Authorize]
    public class InstructorsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly ILocalizationService _localizer;
        private readonly IEmailService _emailService;

        public InstructorsController(AppDbContext context, IConfiguration config, ILocalizationService localizer, IEmailService emailService)
        {
            _context = context;
            _config = config;
            _localizer = localizer;
            _emailService = emailService;
        }

        [HttpGet]
        [RequirePermission(PermissionEnum.Dashboard_Instructors_Read)]
        public async Task<IActionResult> GetInstructors(
            [FromQuery] string? q = null,
            [FromQuery] string? faculty_id = null,
            [FromQuery] string? dept_id = null,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 20)
        {
            var query = _context.Instructors
                .Include(i => i.User)
                .Include(i => i.User!.Faculty)
                .Include(i => i.User!.Department)
                .AsQueryable();

            if (!string.IsNullOrEmpty(faculty_id)) query = query.Where(i => i.User!.FacultyId == faculty_id);
            if (!string.IsNullOrEmpty(dept_id)) query = query.Where(i => i.DepartmentId == dept_id);
            if (!string.IsNullOrEmpty(q))
            {
                var qLower = q.ToLower();
                query = query.Where(i => i.User!.Name.ToLower().Contains(qLower) || i.User!.Email.ToLower().Contains(qLower));
            }

            var total = await query.CountAsync();
            var instructors = await query
                .OrderBy(i => i.User!.Name)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(i => new UserListResponseDto
                {
                    Id = i.UserId,
                    FullName = i.User!.Name,
                    Email = i.User!.Email,
                    Faculty = i.User!.Faculty != null ? new IdNameDto { Id = i.User!.Faculty.Id, Name = i.User!.Faculty.NameAr ?? i.User!.Faculty.Name } : null,
                    Department = i.User!.Department != null ? new IdNameDto { Id = i.User!.Department.Id, Name = i.User!.Department.NameAr ?? i.User!.Department.Name } : null,
                    Status = i.User!.Status,
                    InstructorData = new InstructorDetailsDto
                    {
                        Rank = i.Rank,
                        OfficeHours = i.OfficeHours
                    }
                }).ToListAsync();

            var pagination = new ApiPagination { Page = page, Limit = limit, Total = total, HasMore = (page * limit) < total };
            return Ok(ApiResponse<List<UserListResponseDto>>.CreateSuccess(instructors, pagination: pagination));
        }

        [HttpGet("{id}")]
        [RequirePermission(PermissionEnum.Dashboard_Instructors_Read)]
        public async Task<IActionResult> GetInstructorById(string id)
        {
            var instructor = await _context.Instructors
                .Include(i => i.User)
                    .ThenInclude(u => u!.Faculty)
                .Include(i => i.User)
                    .ThenInclude(u => u!.Department)
                .FirstOrDefaultAsync(i => i.Id == id || i.UserId == id);

            if (instructor == null) return NotFound(ApiResponse<object>.CreateError("INSTRUCTOR_NOT_FOUND", _localizer.GetMessage("USER_NOT_FOUND")));

            var user = instructor.User!;
            var dto = new UserListResponseDto
            {
                Id = user.Id,
                FullName = user.Name,
                Email = user.Email,
                Faculty = user.Faculty != null ? new IdNameDto { Id = user.Faculty.Id, Name = user.Faculty.NameAr ?? user.Faculty.Name } : null,
                Department = user.Department != null ? new IdNameDto { Id = user.Department.Id, Name = user.Department.NameAr ?? user.Department.Name } : null,
                Status = user.Status,
                InstructorData = new InstructorDetailsDto
                {
                    Rank = instructor.Rank,
                    OfficeHours = instructor.OfficeHours
                }
            };

            return Ok(ApiResponse<UserListResponseDto>.CreateSuccess(dto));
        }

        [HttpPost]
        [RequirePermission(PermissionEnum.Dashboard_Instructors_Create)]
        public async Task<IActionResult> CreateInstructor([FromBody] CreateInstructorRequestDto request)
        {
            var exists = await _context.Users.AnyAsync(u => u.Email == request.Email);
            if (exists) return BadRequest(ApiResponse<object>.CreateError("DUPLICATE_EMAIL", _localizer.GetMessage("DUPLICATE_EMAIL")));

            var nidExists = await _context.Users.AnyAsync(u => u.NationalId == request.NationalId);
            if (nidExists) return BadRequest(ApiResponse<object>.CreateError("DUPLICATE_NATIONAL_ID", _localizer.GetMessage("DUPLICATE_NATIONAL_ID")));

            var instRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name.ToLower() == "instructor");
            if (instRole == null) return BadRequest(ApiResponse<object>.CreateError("ROLE_NOT_FOUND", _localizer.GetMessage("ROLE_NOT_FOUND")));
            
            var pepper = _config["PASSWORD_PEPPER"] ?? "";
            var hash = BCrypt.Net.BCrypt.HashPassword(request.NationalId + pepper);

            var user = new User
            {
                Id = "usr_" + Guid.NewGuid().ToString("N").Substring(0, 12),
                Name = request.FullName,
                FullNameAr = request.FullNameAr,
                Email = request.Email,
                NationalId = request.NationalId,
                Phone = request.Phone,
                RoleId = instRole.Id,
                FacultyId = request.FacultyId,
                DepartmentId = request.DepartmentId,
                PasswordHash = hash,
                MustChangePw = true,
                Status = UserStatus.Active
            };

            var instructor = new Instructor
            {
                Id = "ins_" + Guid.NewGuid().ToString("N").Substring(0, 12),
                UserId = user.Id,
                DepartmentId = request.DepartmentId,
                Rank = request.Rank,
                OfficeHours = request.OfficeHours
            };

            user.Instructor = instructor;
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Send Welcome Email (Password is NationalId by default)
            // // Send Welcome Email in background to avoid delay
            // _ = Task.Run(async () =>
            // {
            //     try
            //     {
            //         await _emailService.SendWelcomeEmailAsync(user.Email, user.Name, user.NationalId);
            //     }
            //     catch (Exception)
            //     {
            //         // Fire and forget
            //     }
            // });

            return Created("", ApiResponse<object>.CreateSuccess(new { id = user.Id }));
        }

        [HttpPatch("{id}")]
        [RequirePermission(PermissionEnum.Dashboard_Instructors_Update)]
        public async Task<IActionResult> UpdateInstructor(string id, [FromBody] UpdateInstructorRequestDto request)
        {
            var instructor = await _context.Instructors
                .Include(i => i.User)
                .FirstOrDefaultAsync(i => i.Id == id || i.UserId == id);

            if (instructor == null) return NotFound(ApiResponse<object>.CreateError("INSTRUCTOR_NOT_FOUND", _localizer.GetMessage("USER_NOT_FOUND")));

            var user = instructor.User!;
            if (!string.IsNullOrEmpty(request.Email) && request.Email != user.Email)
            {
                if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                    return BadRequest(ApiResponse<object>.CreateError("DUPLICATE_EMAIL", _localizer.GetMessage("DUPLICATE_EMAIL")));
                user.Email = request.Email;
            }

            if (!string.IsNullOrEmpty(request.FullName)) user.Name = request.FullName;
            if (request.FullNameAr != null) user.FullNameAr = request.FullNameAr;
            if (!string.IsNullOrEmpty(request.NationalId)) user.NationalId = request.NationalId;
            if (request.Phone != null) user.Phone = request.Phone;
            if (request.DepartmentId != null) instructor.DepartmentId = request.DepartmentId;
            if (request.Rank != null) instructor.Rank = request.Rank;
            if (request.OfficeHours != null) instructor.OfficeHours = request.OfficeHours;

            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.CreateSuccess(new { message = _localizer.GetMessage("USER_UPDATED") }));
        }

        [HttpDelete("{id}")]
        [RequirePermission(PermissionEnum.Dashboard_Instructors_Delete)]
        public async Task<IActionResult> DeleteInstructor(string id)
        {
            var instructor = await _context.Instructors.Include(i => i.User).FirstOrDefaultAsync(i => i.Id == id || i.UserId == id);
            if (instructor == null) return NotFound(ApiResponse<object>.CreateError("INSTRUCTOR_NOT_FOUND", _localizer.GetMessage("USER_NOT_FOUND")));

            _context.Users.Remove(instructor.User!);
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.CreateSuccess(new { message = _localizer.GetMessage("DELETED_SUCCESS") }));
        }
    }
}
