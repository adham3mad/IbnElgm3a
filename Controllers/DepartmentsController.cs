using IbnElgm3a.DTOs.Departments;
using IbnElgm3a.DTOs.Common;
using IbnElgm3a.Filters;
using IbnElgm3a.Enums;
using IbnElgm3a.Models;
using IbnElgm3a.Services.Localization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IbnElgm3a.Model;
using IbnElgm3a.Model.Data;
using Microsoft.EntityFrameworkCore;

namespace IbnElgm3a.Controllers
{
    [ApiController]
    [Route("v1/admin/departments")]
    [Authorize]
    public class DepartmentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILocalizationService _localizer;

        public DepartmentsController(AppDbContext context, ILocalizationService localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        private async Task<string> GenerateDepartmentCodeAsync(string facultyId)
        {
            var faculty = await _context.Faculties.FindAsync(facultyId);
            if (faculty == null) throw new System.Exception(_localizer.GetMessage("FACULTY_NOT_FOUND"));

            var codes = await _context.Departments
                .Where(d => d.FacultyId == facultyId)
                .Select(d => d.Code)
                .ToListAsync();

            var numericSeqs = codes
                .Where(c => c.StartsWith(faculty.Code) && c.Length == faculty.Code.Length + 2)
                .Select(c => c.Substring(faculty.Code.Length))
                .Where(s => s.All(char.IsDigit))
                .Select(int.Parse)
                .ToList();

            int nextSeq = 1;
            if (numericSeqs.Any())
            {
                nextSeq = numericSeqs.Max() + 1;
            }

            return faculty.Code + nextSeq.ToString("D2");
        }

        [HttpGet]
        [RequirePermission(PermissionEnum.Dashboard_StructureRead)]
        public async Task<IActionResult> GetDepartments([FromQuery] string? faculty_id = null)
        {
            var query = _context.Departments.AsQueryable();
            if (!string.IsNullOrEmpty(faculty_id)) query = query.Where(d => d.FacultyId == faculty_id);

            var departments = await query
                .Select(d => new DepartmentResponseDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    NameAr = d.NameAr,
                    Code = d.Code,
                    StudentCount = d.StudentCount,
                    InstructorCount = d.InstructorCount,
                    CourseCount = d.CourseCount
                }).ToListAsync();

            return Ok(ApiResponse<List<DepartmentResponseDto>>.CreateSuccess(departments));
        }

        [HttpGet("{id}")]
        [RequirePermission(PermissionEnum.Dashboard_StructureRead)]
        public async Task<IActionResult> GetDepartmentById(string id)
        {
            var dep = await _context.Departments
                .Include(d => d.Faculty)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (dep == null) return NotFound(ApiResponse<object>.CreateError("DEPARTMENT_NOT_FOUND", _localizer.GetMessage("DEPARTMENT_NOT_FOUND")));

            var dto = new DepartmentDetailResponseDto
            {
                Id = dep.Id,
                Name = dep.Name,
                NameAr = dep.NameAr,
                Code = dep.Code,
                StudentCount = dep.StudentCount,
                InstructorCount = dep.InstructorCount,
                CourseCount = dep.CourseCount,
                Faculty = new IdNameDto { Id = dep.FacultyId, Name = dep.Faculty?.NameAr ?? dep.Faculty?.Name ?? "N/A" },
                LevelCount = dep.LevelCount,
                AccentColor = dep.AccentColor
            };

            return Ok(ApiResponse<DepartmentDetailResponseDto>.CreateSuccess(dto));
        }

        [HttpPost]
        [RequirePermission(PermissionEnum.Dashboard_StructureCreate)]
        public async Task<IActionResult> CreateDepartment([FromQuery] string faculty_id, [FromBody] CreateDepartmentRequestDto request)
        {
            var facultyExists = await _context.Faculties.AnyAsync(f => f.Id == faculty_id);
            if (!facultyExists) return NotFound(ApiResponse<object>.CreateError("FACULTY_NOT_FOUND", _localizer.GetMessage("FACULTY_NOT_FOUND")));

            var nameExists = await _context.Departments.AnyAsync(d => d.FacultyId == faculty_id && (d.Name == request.Name || d.NameAr == request.NameAr));
            if (nameExists) return BadRequest(ApiResponse<object>.CreateError("DUPLICATE_NAME", _localizer.GetMessage("DUPLICATE_NAME")));

            if (!string.IsNullOrEmpty(request.DepCode))
            {
                var depCodeExists = await _context.Departments.AnyAsync(d => d.DepCode == request.DepCode);
                if (depCodeExists) return BadRequest(ApiResponse<object>.CreateError("DUPLICATE_DEP_CODE", _localizer.GetMessage("DUPLICATE_DEP_CODE")));
            }

            var code = await GenerateDepartmentCodeAsync(faculty_id);

            var dep = new Department
            {
                Id = "dep_" + System.Guid.NewGuid().ToString("N").Substring(0, 10),
                FacultyId = faculty_id,
                Name = request.Name,
                NameAr = request.NameAr,
                Code = code,
                DepCode = request.DepCode ?? "",
                LevelCount = request.LevelCount,
                HeadUserId = request.HeadUserId,
                AccentColor = request.AccentColor
            };

            _context.Departments.Add(dep);
            await _context.SaveChangesAsync();

            return Created("", ApiResponse<object>.CreateSuccess(new { id = dep.Id }));
        }

        [HttpPatch("{id}")]
        [RequirePermission(PermissionEnum.Dashboard_StructureUpdate)]
        public async Task<IActionResult> UpdateDepartment(string id, [FromBody] UpdateDepartmentRequestDto request)
        {
            var dep = await _context.Departments.FindAsync(id);
            if (dep == null) return NotFound(ApiResponse<object>.CreateError("DEPARTMENT_NOT_FOUND", _localizer.GetMessage("DEPARTMENT_NOT_FOUND")));

            if (!string.IsNullOrEmpty(request.Name) && request.Name != dep.Name)
            {
                if (await _context.Departments.AnyAsync(d => d.FacultyId == dep.FacultyId && d.Name == request.Name))
                    return BadRequest(ApiResponse<object>.CreateError("DUPLICATE_NAME", _localizer.GetMessage("DUPLICATE_NAME")));
                dep.Name = request.Name;
            }
            if (!string.IsNullOrEmpty(request.NameAr) && request.NameAr != dep.NameAr)
            {
                if (await _context.Departments.AnyAsync(d => d.FacultyId == dep.FacultyId && d.NameAr == request.NameAr))
                    return BadRequest(ApiResponse<object>.CreateError("DUPLICATE_NAME", _localizer.GetMessage("DUPLICATE_NAME")));
                dep.NameAr = request.NameAr;
            }
            if (!string.IsNullOrEmpty(request.DepCode) && request.DepCode != dep.DepCode)
            {
                if (await _context.Departments.AnyAsync(d => d.DepCode == request.DepCode))
                    return BadRequest(ApiResponse<object>.CreateError("DUPLICATE_DEP_CODE", _localizer.GetMessage("DUPLICATE_DEP_CODE")));
                dep.DepCode = request.DepCode;
            }
            if (request.LevelCount > 0) dep.LevelCount = request.LevelCount;
            if (request.HeadUserId != null) dep.HeadUserId = request.HeadUserId;
            if (request.AccentColor != null) dep.AccentColor = request.AccentColor;
            
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.CreateSuccess(new { message = _localizer.GetMessage("UPDATED_SUCCESS") }));
        }

        [HttpDelete("{id}")]
        [RequirePermission(PermissionEnum.Dashboard_StructureDelete)]
        public async Task<IActionResult> DeleteDepartment(string id)
        {
            var dep = await _context.Departments.FindAsync(id);
            if (dep == null) return NotFound(ApiResponse<object>.CreateError("DEPARTMENT_NOT_FOUND", _localizer.GetMessage("DEPARTMENT_NOT_FOUND")));

            if (await _context.Users.AnyAsync(u => u.DepartmentId == id))
                return BadRequest(ApiResponse<object>.CreateError("DEPARTMENT_NOT_EMPTY", _localizer.GetMessage("DEPARTMENT_NOT_EMPTY")));

            _context.Departments.Remove(dep);
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.CreateSuccess(new { message = _localizer.GetMessage("DELETED_SUCCESS") }));
        }
    }
}
