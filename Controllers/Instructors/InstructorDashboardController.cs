using IbnElgm3a.Models;
using IbnElgm3a.Models.Data;
using IbnElgm3a.Services;
using IbnElgm3a.Services.Localization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IbnElgm3a.Controllers.Instructors
{
    [ApiController]
    [Route("instructor/dashboard")]
    [Authorize(Roles = "instructor")]
    public class InstructorDashboardController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILocalizationService _localizer;

        public InstructorDashboardController(AppDbContext context, ILocalizationService localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        [HttpGet]
        public async Task<IActionResult> GetDashboard()
        {
            var userId = GetUserId();
            var instructor = await _context.Instructors
                .Include(i => i.User)
                .FirstOrDefaultAsync(i => i.UserId == userId);

            if (instructor == null) return Unauthorized();

            var now = DateTimeOffset.UtcNow;
            var activeSemester = await _context.Semesters
                .Where(s => s.StartDate <= now && s.EndDate >= now)
                .OrderByDescending(s => s.StartDate)
                .FirstOrDefaultAsync() ?? await _context.Semesters.OrderByDescending(s => s.StartDate).FirstOrDefaultAsync();

            if (activeSemester == null)
            {
                return Ok(new { data = new { message = _localizer.GetMessage("NO_ACTIVE_SEMESTER") } });
            }

            var currentWeek = (now - activeSemester.StartDate).Days / 7 + 1;
            if (currentWeek < 1) currentWeek = 1;
            if (currentWeek > activeSemester.TotalWeeks) currentWeek = activeSemester.TotalWeeks;

            // Instructor's Courses
            var activeCourses = await _context.Sections
                .Include(s => s.Course)
                .Where(s => s.InstructorId == instructor.Id && s.Course!.SemesterId == activeSemester.Id)
                .Select(s => s.Course)
                .Distinct()
                .ToListAsync();

            var courseIds = activeCourses.Select(c => c!.Id).ToList();

            // Summary Stats
            var totalStudents = await _context.Enrollments
                .Where(e => courseIds.Contains(e.Section!.CourseId) && e.Status == Enums.EnrollmentStatus.Enrolled)
                .Select(e => e.StudentId)
                .Distinct()
                .CountAsync();

            var submissionsToGrade = await _context.AssignmentSubmissions
                .CountAsync(s => courseIds.Contains(s.Assignment!.CourseId) && s.Status == "submitted");

            // At Risk Students (Attendance < 60%)
            var atRiskCount = await _context.Enrollments
                .Where(e => courseIds.Contains(e.Section!.CourseId) && e.Status == Enums.EnrollmentStatus.Enrolled)
                .CountAsync(e => _context.AttendanceRecords
                    .Where(a => a.StudentId == e.StudentId && courseIds.Contains(a.Session!.Section!.CourseId) && a.Session.AttendanceStatus == "completed")
                    .GroupBy(a => a.StudentId)
                    .Select(g => (double)g.Count(a => a.Status == "present" || a.Status == "late") / g.Count())
                    .FirstOrDefault() < 0.6);

            // Today's Sessions
            var dayOfWeek = (IbnElgm3a.Enums.DayOfWeekEnum)now.DayOfWeek;
            var todaysSessions = await _context.Sessions
                .Include(s => s.Section)
                    .ThenInclude(sec => sec!.Course)
                .Where(s => s.Section!.InstructorId == instructor.Id && s.Date.Date == now.Date)
                .OrderBy(s => s.StartTime)
                .Select(s => new
                {
                    session_id = s.Id,
                    course_id = s.Section!.CourseId,
                    course_code = s.Section.Course!.CourseCode,
                    course_name = s.Section.Course.Title,
                    start_time = s.StartTime,
                    end_time = s.EndTime,
                    room = s.RoomName,
                    student_count = _context.Enrollments.Count(e => e.SectionId == s.SectionId && e.Status == Enums.EnrollmentStatus.Enrolled),
                    attendance_status = s.AttendanceStatus
                })
                .ToListAsync();

            var nameParts = instructor.User?.Name.Split(' ');
            var greetingName = nameParts?.Length > 0 ? nameParts[0] : instructor.User?.Name;

            var response = new
            {
                data = new
                {
                    greeting_name = greetingName,
                    today = new
                    {
                        date = now.ToString("yyyy-MM-dd"),
                        day_name = now.DayOfWeek.ToString(),
                        formatted = now.ToString("dddd, dd MMMM")
                    },
                    semester = new
                    {
                        label = activeSemester.Name,
                        week_current = currentWeek,
                        week_total = activeSemester.TotalWeeks
                    },
                    summary = new
                    {
                        total_courses = activeCourses.Count,
                        total_students = totalStudents,
                        submissions_to_grade = submissionsToGrade,
                        at_risk_students_count = atRiskCount
                    },
                    todays_sessions = todaysSessions,
                    courses = activeCourses.Select(c => new
                    {
                        id = c!.Id,
                        code = c.CourseCode,
                        name = c.Title,
                        semester = activeSemester.Name,
                        week_current = currentWeek,
                        week_total = activeSemester.TotalWeeks,
                        student_count = _context.Enrollments.Count(e => e.Section!.CourseId == c.Id && e.Status == Enums.EnrollmentStatus.Enrolled),
                        status = "active",
                        schedule_summary = _context.ScheduleSlots
                            .Where(ss => ss.Section!.CourseId == c.Id)
                            .Select(ss => ss.Day.ToString().Substring(0, 3) + " " + ss.StartTime)
                            .FirstOrDefault() ?? "",
                        progress_percent = (int)((double)currentWeek / activeSemester.TotalWeeks * 100),
                        pending_submissions_count = _context.AssignmentSubmissions.Count(s => s.Assignment!.CourseId == c.Id && s.Status == "submitted")
                    }).ToList()
                }
            };

            return Ok(response);
        }
    }
}
