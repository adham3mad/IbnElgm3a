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
        public async Task<IActionResult> GetExams([FromQuery] string? type = null, [FromQuery] string? semester_id = null)
        {
            var userId = GetUserId();
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            if (student == null) return Unauthorized();

            var activeSemester = semester_id != null 
                ? await _context.Semesters.FindAsync(semester_id)
                : await _context.Semesters.OrderByDescending(s => s.StartDate).FirstOrDefaultAsync();

            if (activeSemester == null) return NotFound(new { message = _localizer.GetMessage("SEMESTER_NOT_FOUND") });

            var enrollments = await _context.Enrollments
                .Include(e => e.Section)
                    .ThenInclude(sec => sec!.Course)
                .Include(e => e.Section)
                    .ThenInclude(sec => sec!.Instructor)
                        .ThenInclude(i => i!.User)
                .Where(e => e.StudentId == student.Id && e.Status == EnrollmentStatus.Enrolled)
                .ToListAsync();

            var courseIds = enrollments.Select(e => e.Section?.CourseId).Where(id => id != null).ToList();

            var query = _context.Exams
                .Include(e => e.Course)
                .Include(e => e.Hall)
                .Where(e => courseIds.Contains(e.CourseId) && e.SemesterId == activeSemester.Id && e.Status == ExamStatus.Published)
                .AsQueryable();

            if (!string.IsNullOrEmpty(type) && Enum.TryParse<ExamType>(type, true, out var parsedType))
            {
                query = query.Where(e => e.Type == parsedType);
            }

            var exams = await query.ToListAsync();
            var now = DateTimeOffset.UtcNow;

            var nextExamDays = exams.Where(e => e.Date >= now).OrderBy(e => e.Date).FirstOrDefault() != null 
                ? (exams.Where(e => e.Date >= now).OrderBy(e => e.Date).First().Date - now).Days 
                : (int?)null;

            var result = new
            {
                semester_id = activeSemester.Id,
                next_exam_days = nextExamDays,
                exams = exams.Select(e => {
                    var courseEnrollment = enrollments.FirstOrDefault(en => en.Section?.CourseId == e.CourseId);
                    return new
                    {
                        id = e.Id,
                        course_id = e.CourseId,
                        course_code = e.Course?.CourseCode,
                        course_name = e.Course?.Title,
                        type = e.Type.ToString().ToLower(),
                        date = e.Date.ToString("yyyy-MM-dd"),
                        start_time = e.StartTime,
                        end_time = DateTime.ParseExact(e.StartTime, "HH:mm", null).AddMinutes(e.DurationMinutes).ToString("HH:mm"),
                        duration_minutes = e.DurationMinutes,
                        hall = e.Hall?.Name ?? "",
                        floor = "Ground Floor", // static dummy
                        seat_assignment = e.HasSeatPlan ? (object)new {
                            published = true,
                            row = "B", // logic omitted since model doesn't store student-specific seat
                            seat_number = 14,
                            seat_label = "Row B, No. 14",
                            seat_plan_pdf_url = e.SeatPlanPdfUrl
                        } : new { published = false },
                        instructor = courseEnrollment?.Section?.Instructor?.User?.Name ?? "",
                        days_until = (e.Date - now).Days
                    };
                }).ToList()
            };

            return Ok(result);
        }

        [HttpGet("{id}/seat")]
        public async Task<IActionResult> GetExamSeat(string id)
        {
            var userId = GetUserId();
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            if (student == null) return Unauthorized();

            var exam = await _context.Exams
                .Include(e => e.Course)
                .Include(e => e.Hall)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (exam == null) return NotFound(new { error = "not_found", message = _localizer.GetMessage("EXAM_NOT_FOUND") });

            // Ensure enrolled
            var isEnrolled = await _context.Enrollments.AnyAsync(en => en.StudentId == student.Id && en.Section != null && en.Section.CourseId == exam.CourseId && en.Status == EnrollmentStatus.Enrolled);
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
                course_code = exam.Course?.CourseCode,
                course_name = exam.Course?.Title,
                type = exam.Type.ToString().ToLower(),
                date = exam.Date.ToString("yyyy-MM-dd"),
                start_time = exam.StartTime,
                hall = exam.Hall?.Name ?? "",
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
