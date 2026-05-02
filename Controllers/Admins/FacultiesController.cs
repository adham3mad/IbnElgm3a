using IbnElgm3a.DTOs.Faculties;
using IbnElgm3a.DTOs.Departments;
using IbnElgm3a.DTOs.Common;
using IbnElgm3a.Filters;
using IbnElgm3a.Enums;
using IbnElgm3a.Models;
using IbnElgm3a.Services.Localization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IbnElgm3a.Models;
using IbnElgm3a.Models.Data;
using Microsoft.EntityFrameworkCore;

namespace IbnElgm3a.Controllers.Admins
{
    [ApiController]
    [Route("admin/faculties")]
    [Authorize]
    public class FacultiesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILocalizationService _localizer;

        public FacultiesController(AppDbContext context, ILocalizationService localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        private async Task<string> GenerateFacultyCodeAsync()
        {
            var codes = await _context.Faculties
                .Select(f => f.Code)
                .ToListAsync();

            var numericCodes = codes
                .Where(c => c.All(char.IsDigit) && c.Length == 4)
                .Select(int.Parse)
                .ToList();

            int nextCode = 1000;
            if (numericCodes.Any())
            {
                nextCode = numericCodes.Max() + 1;
            }

            return nextCode.ToString("D4");
        }

        [HttpGet]
        [RequirePermission(PermissionEnum.Dashboard_Faculties_Read)]
        public async Task<IActionResult> GetFaculties()
        {
            var faculties = await _context.Faculties
                .Include(f => f.HeadOfFaculty)
                .Include(f => f.Departments)
                .Select(f => new FacultyResponseDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    NameAr = f.NameAr,
                    Code = f.Code,
                    StudentCount = f.StudentCount,
                    HeadOfFaculty = f.HeadOfFaculty != null ? new IdNameDto { Id = f.HeadOfFacultyId ?? "", Name = f.HeadOfFaculty.Name } : null,
                    Departments = f.Departments.Select(d => new DepartmentResponseDto
                    {
                        Id = d.Id,
                        Name = d.Name,
                        NameAr = d.NameAr,
                        Code = d.Code,
                        StudentCount = d.StudentCount,
                        CourseCount = d.CourseCount
                    }).ToList()
                }).ToListAsync();

            return Ok(ApiResponse<List<FacultyResponseDto>>.CreateSuccess(faculties));
        }

        [HttpGet("{id}")]
        [RequirePermission(PermissionEnum.Dashboard_Faculties_Read)]
        public async Task<IActionResult> GetFacultyById(string id)
        {
            var faculty = await _context.Faculties
                .Include(f => f.HeadOfFaculty)
                .Include(f => f.Departments)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (faculty == null) return NotFound(ApiResponse<object>.CreateError("FACULTY_NOT_FOUND", _localizer.GetMessage("FACULTY_NOT_FOUND")));

            var dto = new FacultyDetailResponseDto
            {
                Id = faculty.Id,
                Name = faculty.Name,
                NameAr = faculty.NameAr,
                Code = faculty.Code,
                StudentCount = faculty.StudentCount,
                Building = faculty.Building,
                Email = faculty.OfficialEmail,
                Phone = faculty.OfficialPhone,
                HeadOfFaculty = faculty.HeadOfFaculty != null ? new IdNameDto { Id = faculty.HeadOfFacultyId ?? "", Name = faculty.HeadOfFaculty.Name } : null,
                Settings = new FacultySettingsDto
                {
                    AcceptAdmissions = faculty.AcceptAdmissions,
                    PublicProfile = faculty.PublicProfile,
                    AiChatbotEnabled = faculty.AiChatbotEnabled
                },
                Departments = faculty.Departments.Select(d => new DepartmentResponseDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    NameAr = d.NameAr,
                    Code = d.Code,
                    StudentCount = d.StudentCount,
                    CourseCount = d.CourseCount
                }).ToList()
            };

            return Ok(ApiResponse<FacultyDetailResponseDto>.CreateSuccess(dto));
        }

        [HttpPost]
        [RequirePermission(PermissionEnum.Dashboard_Faculties_Create)]
        public async Task<IActionResult> CreateFaculty([FromBody] CreateFacultyRequestDto request)
        {
            var nameExists = await _context.Faculties.AnyAsync(f => f.Name == request.Name || f.NameAr == request.NameAr);
            if (nameExists) return BadRequest(ApiResponse<object>.CreateError("DUPLICATE_NAME", _localizer.GetMessage("DUPLICATE_NAME")));

            if (!string.IsNullOrEmpty(request.FacCode))
            {
                var facCodeExists = await _context.Faculties.AnyAsync(f => f.FacCode == request.FacCode);
                if (facCodeExists) return BadRequest(ApiResponse<object>.CreateError("DUPLICATE_FAC_CODE", _localizer.GetMessage("DUPLICATE_FAC_CODE")));
            }

            var code = await GenerateFacultyCodeAsync();

            var faculty = new Faculty
            {
                Id = "fac_" + System.Guid.NewGuid().ToString("N").Substring(0, 10),
                Name = request.Name,
                NameAr = request.NameAr,
                Code = code,
                FacCode = request.FacCode ?? "",
                HeadOfFacultyId = request.HeadOfFacultyId,
                Building = request.Building,
                OfficialEmail = request.Email,
                OfficialPhone = request.Phone,
                AcceptAdmissions = request.Settings?.AcceptAdmissions ?? true,
                PublicProfile = request.Settings?.PublicProfile ?? true,
                AiChatbotEnabled = request.Settings?.AiChatbotEnabled ?? false
            };

            _context.Faculties.Add(faculty);
            await _context.SaveChangesAsync();

            return Created("", ApiResponse<object>.CreateSuccess(new { id = faculty.Id }));
        }

        [HttpPatch("{id}")]
        [RequirePermission(PermissionEnum.Dashboard_Faculties_Update)]
        public async Task<IActionResult> UpdateFaculty(string id, [FromBody] UpdateFacultyRequestDto request)
        {
            var faculty = await _context.Faculties.FindAsync(id);
            if (faculty == null) return NotFound(ApiResponse<object>.CreateError("FACULTY_NOT_FOUND", _localizer.GetMessage("FACULTY_NOT_FOUND")));

            if (!string.IsNullOrEmpty(request.Name) && request.Name != faculty.Name)
            {
                if (await _context.Faculties.AnyAsync(f => f.Name == request.Name))
                    return BadRequest(ApiResponse<object>.CreateError("DUPLICATE_NAME", _localizer.GetMessage("DUPLICATE_NAME")));
                faculty.Name = request.Name;
            }
            if (!string.IsNullOrEmpty(request.NameAr) && request.NameAr != faculty.NameAr)
            {
                if (await _context.Faculties.AnyAsync(f => f.NameAr == request.NameAr))
                    return BadRequest(ApiResponse<object>.CreateError("DUPLICATE_NAME", _localizer.GetMessage("DUPLICATE_NAME")));
                faculty.NameAr = request.NameAr;
            }
            if (!string.IsNullOrEmpty(request.FacCode) && request.FacCode != faculty.FacCode)
            {
                if (await _context.Faculties.AnyAsync(f => f.FacCode == request.FacCode))
                    return BadRequest(ApiResponse<object>.CreateError("DUPLICATE_FAC_CODE", _localizer.GetMessage("DUPLICATE_FAC_CODE")));
                faculty.FacCode = request.FacCode;
            }
            if (!string.IsNullOrEmpty(request.HeadOfFacultyId)) faculty.HeadOfFacultyId = request.HeadOfFacultyId;
            if (request.Building != null) faculty.Building = request.Building;
            if (request.Email != null) faculty.OfficialEmail = request.Email;
            if (request.Phone != null) faculty.OfficialPhone = request.Phone;
            if (request.Settings != null)
            {
                faculty.AcceptAdmissions = request.Settings.AcceptAdmissions;
                faculty.PublicProfile = request.Settings.PublicProfile;
                faculty.AiChatbotEnabled = request.Settings.AiChatbotEnabled;
            }

            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.CreateSuccess(new { message = _localizer.GetMessage("UPDATED_SUCCESS") }));
        }

        [HttpDelete("{id}")]
        [RequirePermission(PermissionEnum.Dashboard_Faculties_Delete)]
        public async Task<IActionResult> DeleteFaculty(string id)
        {
            var faculty = await _context.Faculties.FindAsync(id);
            if (faculty == null) return NotFound(ApiResponse<object>.CreateError("FACULTY_NOT_FOUND", _localizer.GetMessage("FACULTY_NOT_FOUND")));

            if (await _context.Departments.AnyAsync(d => d.FacultyId == id))
                return BadRequest(ApiResponse<object>.CreateError("FACULTY_NOT_EMPTY", _localizer.GetMessage("FACULTY_NOT_EMPTY")));

            _context.Faculties.Remove(faculty);
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.CreateSuccess(new { message = _localizer.GetMessage("DELETED_SUCCESS") }));
        }
    }
}
