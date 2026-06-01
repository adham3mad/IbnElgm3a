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

            // Single query: fetch student + active semester + enrollments together
            var studentData = await _context.Students
                .AsNoTracking()
                .Where(s => s.UserId == userId)
                .Select(s => new { s.Id, s.DepartmentId })
                .FirstOrDefaultAsync();

            if (studentData == null) return Unauthorized();

            var activeSemester = semester_id != null
                ? await _context.Semesters.AsNoTracking().Where(s => s.Id == semester_id).Select(s => new { s.Id }).FirstOrDefaultAsync()
                : await _context.Semesters.AsNoTracking().OrderByDescending(s => s.StartDate).Select(s => new { s.Id }).FirstOrDefaultAsync();

            if (activeSemester == null) return Ok(new { semester_id = "", total_credits = 0, courses = new List<object>() });

            var query = _context.Enrollments
                .AsNoTracking()
                .Where(e => e.StudentId == studentData.Id
                    && e.Section != null
                    && e.Section.Course != null
                    && e.Section.Course.SemesterId == activeSemester.Id
                    && e.Status == EnrollmentStatus.Enrolled);

            // Fix: case-insensitive search filter using EF Core string methods
            if (!string.IsNullOrEmpty(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(e =>
                    e.Section!.Course!.Title.ToLower().Contains(lowerSearch) ||
                    e.Section!.Course!.CourseCode.ToLower().Contains(lowerSearch));
            }

            var enrollments = await query
                .Select(e => new
                {
                    CourseId = e.Section!.CourseId,
                    CourseCode = e.Section.Course!.CourseCode,
                    CourseTitle = e.Section.Course.Title,
                    CreditHours = e.Section.Course.CreditHours,
                    ClassType = e.Section.ClassType,
                    SectionName = e.Section.Name,
                    InstructorName = e.Section.Instructor != null && e.Section.Instructor.User != null ? e.Section.Instructor.User.Name : null,
                    InstructorEmail = e.Section.Instructor != null && e.Section.Instructor.User != null ? e.Section.Instructor.User.Email : null,
                    LetterGrade = e.Grade != null ? (LetterGrade?)e.Grade.LetterGrade : null,
                    Marks = e.Grade != null ? (decimal?)e.Grade.Marks : null,
                })
                .ToListAsync();

            var result = new
            {
                semester_id = activeSemester.Id,
                total_credits = enrollments.Sum(e => e.CreditHours),
                courses = enrollments.Select(e => new
                {
                    id = e.CourseId ?? "",
                    code = e.CourseCode ?? "",
                    name = e.CourseTitle ?? "",
                    credit_hours = e.CreditHours,
                    type = e.ClassType.ToString().ToLower(),
                    section = e.SectionName ?? "",
                    instructor = e.InstructorName != null ? new
                    {
                        name = e.InstructorName,
                        email = e.InstructorEmail,
                        office = "TBD"
                    } : (object?)null,
                    attendance_pct = 0,
                    attendance_warning = false,
                    sessions_attended = 0,
                    sessions_total = 0,
                    current_grade = e.LetterGrade.HasValue ? e.LetterGrade.Value.ToString() : "N/A",
                    current_grade_pct = e.Marks ?? 0
                }).ToList()
            };

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCourseById(string id)
        {
            var userId = GetUserId();

            var studentId = await _context.Students
                .AsNoTracking()
                .Where(s => s.UserId == userId)
                .Select(s => s.Id)
                .FirstOrDefaultAsync();

            if (studentId == null) return Unauthorized();

            var enrollment = await _context.Enrollments
                .AsNoTracking()
                .Where(e => e.StudentId == studentId
                    && e.Section != null
                    && e.Section.CourseId == id
                    && e.Status == EnrollmentStatus.Enrolled)
                .Select(e => new
                {
                    CourseId = e.Section!.CourseId,
                    CourseCode = e.Section.Course!.CourseCode,
                    CourseTitle = e.Section.Course.Title,
                    CreditHours = e.Section.Course.CreditHours,
                    ClassType = e.Section.ClassType,
                    SectionName = e.Section.Name,
                    InstructorName = e.Section.Instructor != null && e.Section.Instructor.User != null ? e.Section.Instructor.User.Name : null,
                    InstructorEmail = e.Section.Instructor != null && e.Section.Instructor.User != null ? e.Section.Instructor.User.Email : null,
                    LetterGrade = e.Grade != null ? (LetterGrade?)e.Grade.LetterGrade : null,
                    Marks = e.Grade != null ? (decimal?)e.Grade.Marks : null,
                    ScheduleSlots = e.Section.ScheduleSlots.Select(ss => new
                    {
                        day = ss.Day.ToString(),
                        start_time = ss.StartTime,
                        end_time = ss.EndTime,
                        RoomName = ss.Room != null ? ss.Room.Name : ss.RoomId
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (enrollment == null) return NotFound(new { error = "not_found", message = _localizer.GetMessage("COURSE_NOT_FOUND") });

            var result = new
            {
                id = enrollment.CourseId,
                code = enrollment.CourseCode,
                name = enrollment.CourseTitle,
                credit_hours = enrollment.CreditHours,
                type = enrollment.ClassType.ToString().ToLower(),
                section = enrollment.SectionName,
                instructor = enrollment.InstructorName != null ? new
                {
                    name = enrollment.InstructorName,
                    email = enrollment.InstructorEmail,
                    office = "TBD"
                } : (object?)null,
                grade_breakdown = new List<object>
                {
                    new { component = "Total", weight = 100, score = enrollment.Marks, max = 100, pct = enrollment.Marks, upcoming = false }
                },
                current_total = new
                {
                    score = enrollment.Marks,
                    max = 100,
                    grade = enrollment.LetterGrade.HasValue ? enrollment.LetterGrade.Value.ToString() : null,
                    pct = enrollment.Marks
                },
                attendance_log = new List<object>(),
                schedule_slots = enrollment.ScheduleSlots
            };

            return Ok(result);
        }
    }
}
