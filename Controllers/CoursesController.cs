using IbnElgm3a.DTOs.Courses;
using IbnElgm3a.DTOs.Common;
using IbnElgm3a.Models;
using IbnElgm3a.Filters;
using IbnElgm3a.Enums;
using IbnElgm3a.Services.Localization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IbnElgm3a.Model;
using IbnElgm3a.Model.Data;
using Microsoft.EntityFrameworkCore;

namespace IbnElgm3a.Controllers
{
    [ApiController]
    [Route("v1/admin/courses")]
    [Authorize]
    public class CoursesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILocalizationService _localizer;

        public CoursesController(AppDbContext context, ILocalizationService localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        [HttpGet]
        [RequirePermission(PermissionEnum.Dashboard_Courses_Read)]
        public async Task<IActionResult> GetCourses(
            [FromQuery] string? semester_id = null,
            [FromQuery] string? faculty_id = null,
            [FromQuery] string? department_id = null,
            [FromQuery] string? q = null,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 20)
        {
            var query = _context.Courses
                .Include(c => c.Department)
                .Include(c => c.Sections)
                    .ThenInclude(s => s.Instructor)
                .AsQueryable();

            if (!string.IsNullOrEmpty(department_id)) query = query.Where(c => c.DepartmentId == department_id);
            if (!string.IsNullOrEmpty(q))
            {
                var qLower = q.ToLower();
                query = query.Where(c => c.Title.ToLower().Contains(qLower) || c.CourseCode.ToLower().Contains(qLower));
            }

            var total = await query.CountAsync();
            var courses = await query
                .OrderBy(c => c.Title)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(c => new CourseListResponseDto
                {
                    Id = c.Id,
                    Code = c.CourseCode,
                    Name = c.Title,
                    Department = c.Department != null ? new IdNameDto { Id = c.Department.Id, Name = c.Department.NameAr ?? c.Department.Name } : null,
                    CreditHours = c.CreditHours,
                    EnrolledCount = c.Sections.SelectMany(s => s.Enrollments).Count(),
                    Instructor = c.Sections.FirstOrDefault() != null && c.Sections.First().Instructor != null 
                        ? new IdNameDto { Id = c.Sections.First().Instructor!.UserId, Name = c.Sections.First().Instructor!.User!.Name } 
                        : null,
                    SectionCount = c.Sections.Count
                })
                .ToListAsync();

            var pag = new ApiPagination { Page = page, Limit = limit, Total = total, HasMore = (page * limit) < total };
            return Ok(ApiResponse<List<CourseListResponseDto>>.CreateSuccess(courses, pagination: pag));
        }

        [HttpPost]
        [RequirePermission(PermissionEnum.Dashboard_Courses_Create)]
        public async Task<IActionResult> CreateCourse([FromBody] CreateCourseRequestDto request)
        {
            var exists = await _context.Courses.AnyAsync(c => c.CourseCode == request.Code);
            if (exists) return BadRequest(ApiResponse<object>.CreateError("DUPLICATE_COURSE_CODE", _localizer.GetMessage("DUPLICATE_COURSE_CODE")));

            var titleExists = await _context.Courses.AnyAsync(c => c.Title == request.Name || c.TitleAr == request.NameAr);
            if (titleExists) return BadRequest(ApiResponse<object>.CreateError("DUPLICATE_COURSE_TITLE", _localizer.GetMessage("DUPLICATE_COURSE_TITLE")));

            if (!string.IsNullOrEmpty(request.DepartmentId))
            {
                var depExists = await _context.Departments.AnyAsync(d => d.Id == request.DepartmentId);
                if (!depExists) return BadRequest(ApiResponse<object>.CreateError("DEPARTMENT_NOT_FOUND", _localizer.GetMessage("DEPARTMENT_NOT_FOUND")));
            }

            var course = new Course
            {
                Id = "crs_" + System.Guid.NewGuid().ToString("N").Substring(0, 12),
                CourseCode = request.Code,
                Title = request.Name,
                TitleAr = request.NameAr,
                Description = request.Description,
                CreditHours = request.CreditHours,
                DepartmentId = request.DepartmentId
            };

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            var newCourse = new CourseListResponseDto
            {
                Id = course.Id,
                Code = course.CourseCode,
                Name = course.Title,
                CreditHours = course.CreditHours,
                SectionCount = 0,
                EnrolledCount = 0
            };
            return Created("", newCourse);
        }

        [HttpGet("{course_id}")]
        [RequirePermission(PermissionEnum.Dashboard_Courses_Read)]
        public async Task<IActionResult> GetCourseById(string course_id)
        {
            var course = await _context.Courses
                .Include(c => c.Department)
                .Include(c => c.Sections)
                    .ThenInclude(s => s.Instructor)
                        .ThenInclude(i => i.User)
                .FirstOrDefaultAsync(c => c.Id == course_id);

            if (course == null) return NotFound(ApiResponse<object>.CreateError("COURSE_NOT_FOUND", _localizer.GetMessage("COURSE_NOT_FOUND")));

            var dto = new CourseDetailResponseDto
            {
                Id = course.Id,
                Code = course.CourseCode,
                Name = course.Title,
                NameAr = course.TitleAr,
                Description = course.Description,
                Syllabus = course.Syllabus,
                CreditHours = course.CreditHours,
                Department = course.Department != null ? new IdNameDto { Id = course.Department.Id, Name = course.Department.NameAr ?? course.Department.Name } : null,
                Sections = course.Sections.Select(s => new CourseSectionDto
                {
                    Id = s.Id,
                    InstructorName = s.Instructor?.User?.Name ?? "N/A",
                    RoomName = s.Room ?? "TBD",
                    Day = s.DayOfWeek != null && System.Enum.TryParse<DayOfWeek>(s.DayOfWeek, true, out var d) ? d : DayOfWeek.Monday, // Placeholder logic
                    StartTime = s.StartTime.ToString(@"hh\:mm"),
                    Capacity = s.Capacity,
                    Enrolled = s.Enrollments.Count
                }).ToList()
            };

            return Ok(ApiResponse<CourseDetailResponseDto>.CreateSuccess(dto));
        }

        [HttpPatch("{course_id}")]
        [RequirePermission(PermissionEnum.Dashboard_Courses_Update)]
        public async Task<IActionResult> UpdateCourse(string course_id, [FromBody] UpdateCourseRequestDto request)
        {
            var course = await _context.Courses.FindAsync(course_id);
            if (course == null) return NotFound(ApiResponse<object>.CreateError("COURSE_NOT_FOUND", _localizer.GetMessage("COURSE_NOT_FOUND")));

            if (!string.IsNullOrEmpty(request.Code)) course.CourseCode = request.Code;
            if (!string.IsNullOrEmpty(request.Name)) course.Title = request.Name;
            if (request.NameAr != null) course.TitleAr = request.NameAr;
            if (!string.IsNullOrEmpty(request.DepartmentId))
            {
                var depExists = await _context.Departments.AnyAsync(d => d.Id == request.DepartmentId);
                if (!depExists) return BadRequest(ApiResponse<object>.CreateError("DEPARTMENT_NOT_FOUND", _localizer.GetMessage("DEPARTMENT_NOT_FOUND")));
                course.DepartmentId = request.DepartmentId;
            }
            if (request.CreditHours.HasValue) course.CreditHours = request.CreditHours.Value;
            if (request.SemesterId != null) course.SemesterId = request.SemesterId;
            if (request.InstructorId != null) course.InstructorId = request.InstructorId;
            if (request.Description != null) course.Description = request.Description;
            if (request.Syllabus != null) course.Syllabus = request.Syllabus;
            if (request.MaxStudents.HasValue) course.MaxStudents = request.MaxStudents.Value;

            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.CreateSuccess(new { message = _localizer.GetMessage("UPDATED_SUCCESS") }));
        }

        [HttpDelete("{course_id}")]
        [RequirePermission(PermissionEnum.Dashboard_Courses_Delete)]
        public async Task<IActionResult> DeleteCourse(string course_id)
        {
            var course = await _context.Courses.FindAsync(course_id);
            if (course == null) return NotFound(ApiResponse<object>.CreateError("COURSE_NOT_FOUND", _localizer.GetMessage("COURSE_NOT_FOUND")));

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.CreateSuccess(new { message = _localizer.GetMessage("DELETED_SUCCESS") }));
        }
    }
}
