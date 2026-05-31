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
        private readonly IServiceScopeFactory _scopeFactory;

        public InstructorDashboardController(AppDbContext context, ILocalizationService localizer, IServiceScopeFactory scopeFactory)
        {
            _context = context;
            _localizer = localizer;
            _scopeFactory = scopeFactory;
        }

        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        [HttpGet]
        public async Task<IActionResult> GetDashboard()
        {
            var userId = GetUserId();

            // Round 1: Fetch instructor and semesters in parallel using separate scopes
            var instructorTask = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                return await db.Instructors
                    .AsNoTracking()
                    .Include(i => i.User)
                    .FirstOrDefaultAsync(i => i.UserId == userId);
            });

            var semestersTask = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                return await db.Semesters
                    .AsNoTracking()
                    .OrderByDescending(s => s.StartDate)
                    .ToListAsync();
            });

            await Task.WhenAll(instructorTask, semestersTask);

            var instructor = await instructorTask;
            if (instructor == null)
            {
                return Unauthorized(ApiResponse<object>.CreateError("UNAUTHORIZED", _localizer.GetMessage("UNAUTHORIZED")));
            }

            var semesters = await semestersTask;
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

            // Instructor's Courses
            var activeCourses = await _context.Sections
                .AsNoTracking()
                .Include(s => s.Course)
                .Where(s => s.InstructorId == instructor.Id && s.Course!.SemesterId == activeSemester.Id)
                .Select(s => s.Course)
                .Distinct()
                .ToListAsync();

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

            // Round 2 Parallel Queries:
            var totalStudentsTask = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                return await db.Enrollments
                    .AsNoTracking()
                    .Where(e => courseIds.Contains(e.Section!.CourseId) && e.Status == Enums.EnrollmentStatus.Enrolled)
                    .Select(e => e.StudentId)
                    .Distinct()
                    .CountAsync();
            });

            var submissionsToGradeTask = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                return await db.AssignmentSubmissions
                    .AsNoTracking()
                    .CountAsync(s => courseIds.Contains(s.Assignment!.CourseId) && s.Status == "submitted");
            });

            var atRiskStatsTask = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                // Project stats to avoid untranslatable nested GroupBy inside Count predicate
                return await db.Enrollments
                    .AsNoTracking()
                    .Where(e => courseIds.Contains(e.Section!.CourseId) && e.Status == Enums.EnrollmentStatus.Enrolled)
                    .Select(e => new
                    {
                        StudentId = e.StudentId,
                        TotalSessions = db.AttendanceRecords.Count(a => a.StudentId == e.StudentId && courseIds.Contains(a.Session!.Section!.CourseId) && a.Session.AttendanceStatus == "completed"),
                        PresentSessions = db.AttendanceRecords.Count(a => a.StudentId == e.StudentId && courseIds.Contains(a.Session!.Section!.CourseId) && a.Session.AttendanceStatus == "completed" && (a.Status == "present" || a.Status == "late"))
                    })
                    .ToListAsync();
            });

            var todaysSessionsTask = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                return await db.Sessions
                    .AsNoTracking()
                    .Include(s => s.Section)
                        .ThenInclude(sec => sec!.Course)
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
                        student_count = db.Enrollments.Count(e => e.SectionId == s.SectionId && e.Status == Enums.EnrollmentStatus.Enrolled),
                        attendance_status = s.AttendanceStatus
                    })
                    .ToListAsync();
            });

            var courseCountsTask = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                return await db.Sections
                    .AsNoTracking()
                    .Where(s => s.InstructorId == instructor.Id && courseIds.Contains(s.CourseId))
                    .Select(s => new
                    {
                        CourseId = s.CourseId,
                        StudentCount = db.Enrollments.Count(e => e.SectionId == s.Id && e.Status == Enums.EnrollmentStatus.Enrolled),
                        PendingCount = db.AssignmentSubmissions.Count(sub => sub.Assignment!.CourseId == s.CourseId && sub.Status == "submitted")
                    })
                    .ToListAsync();
            });

            var scheduleSlotsTask = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                // Fetch to project Day.ToString() in memory to avoid Postgres translation error on Enum to string conversions
                return await db.ScheduleSlots
                    .AsNoTracking()
                    .Where(ss => courseIds.Contains(ss.Section!.CourseId))
                    .Select(ss => new { ss.Section!.CourseId, ss.Day, ss.StartTime })
                    .ToListAsync();
            });

            await Task.WhenAll(
                totalStudentsTask,
                submissionsToGradeTask,
                atRiskStatsTask,
                todaysSessionsTask,
                courseCountsTask,
                scheduleSlotsTask
            );

            var totalStudents = await totalStudentsTask;
            var submissionsToGrade = await submissionsToGradeTask;
            var atRiskStats = await atRiskStatsTask;
            var todaysSessions = await todaysSessionsTask;
            var courseCounts = await courseCountsTask;
            var scheduleSlots = await scheduleSlotsTask;

            var atRiskCount = atRiskStats.Count(s => s.TotalSessions > 0 && (double)s.PresentSessions / s.TotalSessions < 0.6);

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
                    var countData = courseCounts.FirstOrDefault(cc => cc.CourseId == c!.Id);
                    var slot = scheduleSlots.FirstOrDefault(ss => ss.CourseId == c!.Id);
                    var scheduleSummary = slot != null
                        ? slot.Day.ToString().Substring(0, Math.Min(3, slot.Day.ToString().Length)) + " " + slot.StartTime
                        : "";

                    return new
                    {
                        id = c!.Id,
                        code = c.CourseCode,
                        name = c.Title,
                        semester = activeSemester.Name,
                        week_current = currentWeek,
                        week_total = activeSemester.TotalWeeks,
                        student_count = countData?.StudentCount ?? 0,
                        status = semesterStatus,
                        schedule_summary = scheduleSummary,
                        progress_percent = activeSemester.TotalWeeks > 0 ? (int)((double)currentWeek / activeSemester.TotalWeeks * 100) : 0,
                        pending_submissions_count = countData?.PendingCount ?? 0
                    };
                }).ToList()
            };

            return Ok(response);
        }
    }
}
