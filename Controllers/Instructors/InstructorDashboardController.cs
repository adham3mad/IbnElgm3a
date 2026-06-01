using IbnElgm3a.Models;
using IbnElgm3a.Models.Data;
using IbnElgm3a.Services;
using IbnElgm3a.Services.Localization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;

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

            // 1. Fetch instructor details (Fast, single query on same context)
            var instructor = await _context.Instructors
                .AsNoTracking()
                .Include(i => i.User)
                .FirstOrDefaultAsync(i => i.UserId == userId);

            if (instructor == null)
            {
                return Unauthorized(ApiResponse<object>.CreateError("UNAUTHORIZED", _localizer.GetMessage("UNAUTHORIZED")));
            }

            // 2. Fetch semesters list (Fast, simple query)
            var semesters = await _context.Semesters
                .AsNoTracking()
                .OrderByDescending(s => s.StartDate)
                .ToListAsync();

            var now = DateTimeOffset.UtcNow;
            var todayUtc = DateTime.SpecifyKind(now.Date, DateTimeKind.Utc);
            var tomorrowUtc = todayUtc.AddDays(1);
            var activeSemester = semesters.FirstOrDefault(s => s.StartDate <= now && s.EndDate >= now)
                ?? semesters.FirstOrDefault();

            if (activeSemester == null)
            {
                return NotFound(ApiResponse<object>.CreateError("NO_ACTIVE_SEMESTER", _localizer.GetMessage("NO_ACTIVE_SEMESTER")));
            }

            var currentWeek = (now - activeSemester.StartDate).Days / 7 + 1;
            if (currentWeek < 1) currentWeek = 1;
            if (currentWeek > activeSemester.TotalWeeks) currentWeek = activeSemester.TotalWeeks;

            // 3. Fetch instructor's active sections, course details, enrollment counts, schedule slots, and pending submissions count in a single query.
            var sectionsData = await _context.Sections
                .AsNoTracking()
                .Where(s => s.InstructorId == instructor.Id && s.Course!.SemesterId == activeSemester.Id)
                .Select(s => new
                {
                    SectionId = s.Id,
                    Course = s.Course,
                    StudentCount = s.Enrollments.Count(e => e.Status == Enums.EnrollmentStatus.Enrolled),
                    PendingCount = _context.AssignmentSubmissions.Count(sub => sub.Assignment!.CourseId == s.CourseId && sub.Status == "submitted"),
                    ScheduleSlots = s.ScheduleSlots.Select(ss => new { ss.Day, ss.StartTime }).ToList()
                })
                .ToListAsync();

            var activeCourses = sectionsData
                .Select(s => s.Course)
                .Where(c => c != null)
                .DistinctBy(c => c!.Id)
                .ToList();

            var courseIds = activeCourses.Select(c => c!.Id).ToList();

            if (!courseIds.Any())
            {
                var namePartsEmpty = instructor.User?.Name.Split(' ');
                var greetingNameEmpty = namePartsEmpty?.Length > 0 ? namePartsEmpty[0] : instructor.User?.Name;

                return Ok(new
                {
                    greeting_name = greetingNameEmpty,
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
                        total_courses = 0,
                        total_students = 0,
                        submissions_to_grade = 0,
                        at_risk_students_count = 0
                    },
                    todays_sessions = Array.Empty<object>(),
                    courses = Array.Empty<object>()
                });
            }

            // 4. Fetch total unique students (Fast index query)
            var sectionIds = sectionsData.Select(s => s.SectionId).ToList();
            var totalStudents = await _context.Enrollments
                .AsNoTracking()
                .Where(e => sectionIds.Contains(e.SectionId) && e.Status == Enums.EnrollmentStatus.Enrolled)
                .Select(e => e.StudentId)
                .Distinct()
                .CountAsync();

            // 5. Fetch total submissions to grade (Fast index query)
            var submissionsToGrade = await _context.AssignmentSubmissions
                .AsNoTracking()
                .CountAsync(s => courseIds.Contains(s.Assignment!.CourseId) && s.Status == "submitted");

            // 6. Fetch at-risk students by grouping attendance records (Highly performant, 100% translatable)
            var atRiskStats = await _context.AttendanceRecords
                .AsNoTracking()
                .Where(a => courseIds.Contains(a.Session!.Section!.CourseId) && a.Session.AttendanceStatus == "completed")
                .GroupBy(a => a.StudentId)
                .Select(g => new
                {
                    StudentId = g.Key,
                    TotalSessions = g.Count(),
                    PresentSessions = g.Count(a => a.Status == "present" || a.Status == "late")
                })
                .ToListAsync();

            var atRiskCount = atRiskStats.Count(s => s.TotalSessions > 0 && (double)s.PresentSessions / s.TotalSessions < 0.6);

            // 7. Fetch today's sessions (Fast query)
            var todaysSessions = await _context.Sessions
                .AsNoTracking()
                .Where(s => s.Section!.InstructorId == instructor.Id && s.Date >= todayUtc && s.Date < tomorrowUtc)
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

            var semesterStatus = activeSemester.StartDate <= now && activeSemester.EndDate >= now
                ? "active"
                : (activeSemester.EndDate < now ? "archived" : "upcoming");

            var response = new
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
                courses = activeCourses.Select(c =>
                {
                    // Aggregate stats for this course across its sections
                    var courseSections = sectionsData.Where(sd => sd.Course!.Id == c!.Id).ToList();
                    var studentCount = courseSections.Sum(cs => cs.StudentCount);
                    var pendingCount = courseSections.FirstOrDefault()?.PendingCount ?? 0;
                    
                    var firstSlot = courseSections
                        .SelectMany(cs => cs.ScheduleSlots)
                        .FirstOrDefault();

                    var scheduleSummary = firstSlot != null
                        ? firstSlot.Day.ToString().Substring(0, Math.Min(3, firstSlot.Day.ToString().Length)) + " " + firstSlot.StartTime
                        : "";

                    return new
                    {
                        id = c!.Id,
                        code = c.CourseCode,
                        name = c.Title,
                        semester = activeSemester.Name,
                        week_current = currentWeek,
                        week_total = activeSemester.TotalWeeks,
                        student_count = studentCount,
                        status = semesterStatus,
                        schedule_summary = scheduleSummary,
                        progress_percent = activeSemester.TotalWeeks > 0 ? (int)((double)currentWeek / activeSemester.TotalWeeks * 100) : 0,
                        pending_submissions_count = pendingCount
                    };
                }).ToList()
            };

            return Ok(response);
        }
    }
}
