using IbnElgm3a.Models;
using IbnElgm3a.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace IbnElgm3a.Controllers.Students
{
    [ApiController]
    [Route("student/courses")]
    [Authorize(Roles = "student")]
    public class StudentCoursesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IbnElgm3a.Services.Localization.ILocalizationService _localizer;

        public StudentCoursesController(AppDbContext context, IbnElgm3a.Services.Localization.ILocalizationService localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        private string GetUserId() => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";

        [HttpGet]
        public async Task<IActionResult> GetCourses([FromQuery] string? semester_id = null, [FromQuery] string? search = null)
        {
            var userId = GetUserId();
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            if (student == null) return Unauthorized();

            var activeSemester = semester_id != null 
                ? await _context.Semesters.FindAsync(semester_id)
                : await _context.Semesters.OrderByDescending(s => s.StartDate).FirstOrDefaultAsync();

            if (activeSemester == null) return Ok(new { semester_id = "", total_credits = 0, courses = new List<object>() });

            var query = _context.Enrollments
                .Include(e => e.Section)
                    .ThenInclude(s => s!.Course)
                .Include(e => e.Section)
                    .ThenInclude(s => s!.Instructor)
                        .ThenInclude(i => i!.User)
                .Include(e => e.Grade)
                .Where(e => e.StudentId == student.Id && e.Section != null && e.Section.Course != null && e.Section.Course.SemesterId == activeSemester.Id && e.Status == EnrollmentStatus.Enrolled)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(e => e.Section!.Course!.Title.ToLower().Contains(lowerSearch) || e.Section!.Course!.CourseCode.ToLower().Contains(lowerSearch));
            }

            var enrollments = await query.ToListAsync();

            var result = new
            {
                semester_id = activeSemester.Id,
                total_credits = enrollments.Sum(e => e.Section?.Course?.CreditHours ?? 0),
                courses = enrollments.Select(e => new
                {
                    id = e.Section?.CourseId ?? "",
                    code = e.Section?.Course?.CourseCode ?? "",
                    name = e.Section?.Course?.Title ?? "",
                    credit_hours = e.Section?.Course?.CreditHours ?? 0,
                    type = e.Section?.ClassType.ToString().ToLower() ?? "lecture",
                    section = e.Section?.Name ?? "",
                    instructor = e.Section?.Instructor?.User != null ? new {
                        name = e.Section.Instructor.User.Name,
                        email = e.Section.Instructor.User.Email,
                        office = "TBD" 
                    } : null,
                    attendance_pct = 0, 
                    attendance_warning = false,
                    sessions_attended = 0,
                    sessions_total = 0,
                    current_grade = e.Grade?.LetterGrade.ToString() ?? "N/A",
                    current_grade_pct = e.Grade != null ? e.Grade.Marks : 0
                }).ToList()
            };

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCourseById(string id)
        {
            var userId = GetUserId();
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            if (student == null) return Unauthorized();

            var enrollment = await _context.Enrollments
                .Include(e => e.Section)
                    .ThenInclude(s => s!.Course)
                .Include(e => e.Section)
                    .ThenInclude(s => s!.Instructor)
                        .ThenInclude(i => i!.User)
                .Include(e => e.Section)
                    .ThenInclude(s => s!.ScheduleSlots)
                        .ThenInclude(ss => ss.Room)
                .Include(e => e.Grade)
                .FirstOrDefaultAsync(e => e.StudentId == student.Id && e.Section != null && e.Section.CourseId == id && e.Status == EnrollmentStatus.Enrolled);

            if (enrollment == null) return NotFound(new { error = "not_found", message = _localizer.GetMessage("COURSE_NOT_FOUND") });

            var result = new
            {
                id = enrollment.Section?.CourseId,
                code = enrollment.Section?.Course?.CourseCode,
                name = enrollment.Section?.Course?.Title,
                credit_hours = enrollment.Section?.Course?.CreditHours,
                type = enrollment.Section?.ClassType.ToString().ToLower() ?? "lecture",
                section = enrollment.Section?.Name,
                instructor = enrollment.Section?.Instructor?.User != null ? new {
                    name = enrollment.Section.Instructor.User.Name,
                    email = enrollment.Section.Instructor.User.Email,
                    office = "TBD"
                } : null,
                grade_breakdown = new List<object>
                {
                    new { component = "Total", weight = 100, score = enrollment.Grade?.Marks, max = 100, pct = enrollment.Grade?.Marks, upcoming = false }
                },
                current_total = new
                {
                    score = enrollment.Grade?.Marks,
                    max = 100,
                    grade = enrollment.Grade?.LetterGrade.ToString(),
                    pct = enrollment.Grade?.Marks
                },
                attendance_log = new List<object>(), 
                schedule_slots = enrollment.Section?.ScheduleSlots.Select(s => new
                {
                    day = s.Day.ToString(),
                    start_time = s.StartTime,
                    end_time = s.EndTime,
                    room = s.Room?.Name ?? s.RoomId
                }).ToList()
            };

            return Ok(result);
        }
    }
}
