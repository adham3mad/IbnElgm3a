using IbnElgm3a.Models;
using IbnElgm3a.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace IbnElgm3a.Controllers.Students
{
    [ApiController]
    [Route("student/exams")]
    [Authorize(Roles = "student")]
    public class StudentExamsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IbnElgm3a.Services.Localization.ILocalizationService _localizer;

        public StudentExamsController(AppDbContext context, IbnElgm3a.Services.Localization.ILocalizationService localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        private string GetUserId() => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";

        [HttpGet]
        [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Client)]
        public async Task<IActionResult> GetExams([FromQuery] ExamType? type = null, [FromQuery] string? semester_id = null)
        {
            var userId = GetUserId();
            var now = DateTimeOffset.UtcNow;

            // Single query: fetch student ID
            var studentId = await _context.Students
                .AsNoTracking()
                .Where(s => s.UserId == userId)
                .Select(s => s.Id)
                .FirstOrDefaultAsync();

            if (studentId == null) return Unauthorized();

            // Fetch active semester (lightweight)
            var activeSemesterId = semester_id != null
                ? semester_id
                : await _context.Semesters
                    .AsNoTracking()
                    .OrderByDescending(s => s.StartDate)
                    .Select(s => s.Id)
                    .FirstOrDefaultAsync();

            if (activeSemesterId == null) return NotFound(new { message = _localizer.GetMessage("SEMESTER_NOT_FOUND") });

            // Build exam query with enrolled-course filter using sub-select
            var examQuery = _context.Exams
                .AsNoTracking()
                .Where(e => e.SemesterId == activeSemesterId
                    && e.Status == ExamStatus.Published
                    && _context.Enrollments.Any(en =>
                        en.StudentId == studentId
                        && en.Section!.CourseId == e.CourseId
                        && en.Status == EnrollmentStatus.Enrolled));

            // Apply type filter if provided
            if (type.HasValue)
            {
                examQuery = examQuery.Where(e => e.Type == type.Value);
            }

            // Fetch exams with projection (no Include)
            var exams = await examQuery
                .Select(e => new
                {
                    Id = e.Id,
                    CourseId = e.CourseId,
                    CourseCode = e.Course != null ? e.Course.CourseCode : null,
                    CourseTitle = e.Course != null ? e.Course.Title : null,
                    Type = e.Type,
                    Date = e.Date,
                    StartTime = e.StartTime,
                    DurationMinutes = e.DurationMinutes,
                    HallName = e.Hall != null ? e.Hall.Name : null,
                    HasSeatPlan = e.HasSeatPlan,
                    SeatPlanPdfUrl = e.SeatPlanPdfUrl,
                    InstructorName = _context.Enrollments
                        .Where(en => en.StudentId == studentId && en.Section!.CourseId == e.CourseId && en.Status == EnrollmentStatus.Enrolled)
                        .Select(en => en.Section!.Instructor != null && en.Section.Instructor.User != null ? en.Section.Instructor.User.Name : null)
                        .FirstOrDefault()
                })
                .ToListAsync();

            var nextExamDays = exams
                .Where(e => e.Date >= now)
                .OrderBy(e => e.Date)
                .Select(e => (int?)((e.Date - now).Days))
                .FirstOrDefault();

            var result = new
            {
                semester_id = activeSemesterId,
                next_exam_days = nextExamDays,
                exams = exams.Select(e => new
                {
                    id = e.Id,
                    course_id = e.CourseId,
                    course_code = e.CourseCode,
                    course_name = e.CourseTitle,
                    type = e.Type.ToString().ToLower(),
                    date = e.Date.ToString("yyyy-MM-dd"),
                    start_time = e.StartTime,
                    end_time = DateTime.ParseExact(e.StartTime, "HH:mm", null).AddMinutes(e.DurationMinutes).ToString("HH:mm"),
                    duration_minutes = e.DurationMinutes,
                    hall = e.HallName ?? "",
                    floor = "Ground Floor",
                    seat_assignment = e.HasSeatPlan ? (object)new
                    {
                        published = true,
                        row = "B",
                        seat_number = 14,
                        seat_label = "Row B, No. 14",
                        seat_plan_pdf_url = e.SeatPlanPdfUrl
                    } : new { published = false },
                    instructor = e.InstructorName ?? "",
                    days_until = (e.Date - now).Days
                }).ToList()
            };

            return Ok(result);
        }

        [HttpGet("{id}/seat")]
        public async Task<IActionResult> GetExamSeat(string id)
        {
            var userId = GetUserId();

            var studentId = await _context.Students
                .AsNoTracking()
                .Where(s => s.UserId == userId)
                .Select(s => s.Id)
                .FirstOrDefaultAsync();

            if (studentId == null) return Unauthorized();

            var exam = await _context.Exams
                .AsNoTracking()
                .Where(e => e.Id == id)
                .Select(e => new
                {
                    e.Id,
                    CourseId = e.CourseId,
                    CourseCode = e.Course != null ? e.Course.CourseCode : null,
                    CourseTitle = e.Course != null ? e.Course.Title : null,
                    e.Type,
                    e.Date,
                    e.StartTime,
                    HallName = e.Hall != null ? e.Hall.Name : null,
                    e.HasSeatPlan,
                    e.SeatPlanPdfUrl,
                    e.LayoutUrl
                })
                .FirstOrDefaultAsync();

            if (exam == null) return NotFound(new { error = "not_found", message = _localizer.GetMessage("EXAM_NOT_FOUND") });

            // Ensure enrolled
            var isEnrolled = await _context.Enrollments
                .AsNoTracking()
                .AnyAsync(en => en.StudentId == studentId
                    && en.Section != null
                    && en.Section.CourseId == exam.CourseId
                    && en.Status == EnrollmentStatus.Enrolled);

            if (!isEnrolled) return NotFound(new { error = "not_found", message = _localizer.GetMessage("EXAM_NOT_FOUND") });

            if (!exam.HasSeatPlan)
            {
                return Ok(new
                {
                    exam_id = exam.Id,
                    seat_assignment = new
                    {
                        published = false,
                        message = _localizer.GetMessage("SEAT_PLAN_NOT_PUBLISHED")
                    }
                });
            }

            return Ok(new
            {
                exam_id = exam.Id,
                course_code = exam.CourseCode,
                course_name = exam.CourseTitle,
                type = exam.Type.ToString().ToLower(),
                date = exam.Date.ToString("yyyy-MM-dd"),
                start_time = exam.StartTime,
                hall = exam.HallName ?? "",
                floor = "Ground Floor",
                seat_assignment = new
                {
                    published = true,
                    row = "B",
                    seat_number = 14,
                    seat_label = "Row B, No. 14",
                    hall_map_url = exam.LayoutUrl ?? "https://cdn.masaar.edu.eg/hall-maps/hall_default.png",
                    seat_plan_pdf_url = exam.SeatPlanPdfUrl
                }
            });
        }
    }
}
