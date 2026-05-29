using IbnElgm3a.Models;
using IbnElgm3a.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using IbnElgm3a.Services;
using Microsoft.Extensions.Caching.Memory;

namespace IbnElgm3a.Controllers.Students
{
    [ApiController]
    [Route("student/dashboard")]
    [Authorize(Roles = "student")]
    public class StudentDashboardController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IbnElgm3a.Services.Localization.ILocalizationService _localizer;
        private readonly Microsoft.Extensions.DependencyInjection.IServiceScopeFactory _scopeFactory;
        private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _cache;

        public StudentDashboardController(
            AppDbContext context, 
            INotificationService notificationService, 
            IbnElgm3a.Services.Localization.ILocalizationService localizer,
            Microsoft.Extensions.DependencyInjection.IServiceScopeFactory scopeFactory,
            Microsoft.Extensions.Caching.Memory.IMemoryCache cache)
        {
            _context = context;
            _notificationService = notificationService;
            _localizer = localizer;
            _scopeFactory = scopeFactory;
            _cache = cache;
        }

        private string GetUserId() => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";

        [HttpGet]
        [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Client)]
        public async Task<IActionResult> GetDashboard()
        {
            var userId = GetUserId();
            var cacheKey = $"student_dashboard_{userId}";

            if (_cache.TryGetValue(cacheKey, out object? cachedResponse) && cachedResponse != null)
            {
                return Ok(cachedResponse);
            }

            var now = DateTimeOffset.UtcNow;

            // Round 1: Fetch student profile and semesters in parallel using separate scopes
            var studentTask = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                return await db.Students
                    .AsNoTracking()
                    .Include(s => s.User)
                        .ThenInclude(u => u!.Faculty)
                    .Include(s => s.User)
                        .ThenInclude(u => u!.Department)
                    .FirstOrDefaultAsync(s => s.UserId == userId);
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

            await Task.WhenAll(studentTask, semestersTask);

            var student = await studentTask;
            if (student == null) return Unauthorized(new { message = _localizer.GetMessage("UNAUTHORIZED") });

            var semesters = await semestersTask;
            var activeSemester = semesters.FirstOrDefault();
            var nextSemester = semesters
                .Where(s => s.StartDate > now)
                .OrderBy(s => s.StartDate)
                .FirstOrDefault();

            // Round 2: Fetch everything else in parallel using activeSemester.Id and student.Id
            var countsTask = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                return await db.Students
                    .AsNoTracking()
                    .Where(s => s.Id == student.Id)
                    .Select(s => new
                    {
                        OpenComplaints = db.Complaints.Count(c => c.StudentId == s.Id && c.Status != ComplaintStatus.Resolved && c.Status != ComplaintStatus.Closed),
                        UnreadNotifications = db.Notifications.Count(n => n.StudentId == s.Id && !n.IsRead),
                        AttendedCount = activeSemester != null ? db.AttendanceRecords.Count(a => a.StudentId == s.Id && a.Session!.Section!.Course!.SemesterId == activeSemester.Id && a.Session.AttendanceStatus == "completed" && (a.Status == "present" || a.Status == "late")) : 0,
                        TotalCompletedSessions = activeSemester != null ? db.Sessions.Count(sess => sess.AttendanceStatus == "completed" && db.Enrollments.Any(e => e.StudentId == s.Id && e.SectionId == sess.SectionId && e.Status == EnrollmentStatus.Enrolled && e.Section!.Course!.SemesterId == activeSemester.Id)) : 0
                    })
                    .FirstOrDefaultAsync();
            });

            var enrollStatsTask = Task.Run(async () =>
            {
                if (activeSemester == null) return (Count: 0, CreditHours: 0);
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var stats = await db.Enrollments
                    .AsNoTracking()
                    .Where(e => e.StudentId == student.Id && e.Section != null && e.Section.Course != null && e.Section.Course.SemesterId == activeSemester.Id && e.Status == EnrollmentStatus.Enrolled)
                    .GroupBy(e => 1)
                    .Select(g => new
                    {
                        Count = g.Count(),
                        CreditHours = g.Sum(e => e.Section!.Course!.CreditHours)
                    })
                    .FirstOrDefaultAsync();

                return stats != null ? (Count: stats.Count, CreditHours: stats.CreditHours) : (Count: 0, CreditHours: 0);
            });

            var examsTask = Task.Run(async () =>
            {
                if (activeSemester == null) return (Count: 0, NextDate: (DateTimeOffset?)null);
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var upcomingExamsQuery = db.Exams
                    .AsNoTracking()
                    .Where(e => e.Date >= now && db.Enrollments.Any(en => en.StudentId == student.Id && en.Section!.CourseId == e.CourseId && en.Status == EnrollmentStatus.Enrolled && en.Section.Course.SemesterId == activeSemester.Id));

                var count = await upcomingExamsQuery.CountAsync();
                var nextDate = await upcomingExamsQuery
                    .OrderBy(e => e.Date)
                    .Select(e => (DateTimeOffset?)e.Date)
                    .FirstOrDefaultAsync();

                return (Count: count, NextDate: nextDate);
            });

            var dayOfWeek = now.DayOfWeek;
            DayOfWeekEnum currentEnumDay = dayOfWeek switch
            {
                DayOfWeek.Saturday => DayOfWeekEnum.Saturday,
                DayOfWeek.Sunday => DayOfWeekEnum.Sunday,
                DayOfWeek.Monday => DayOfWeekEnum.Monday,
                DayOfWeek.Tuesday => DayOfWeekEnum.Tuesday,
                DayOfWeek.Wednesday => DayOfWeekEnum.Wednesday,
                DayOfWeek.Thursday => DayOfWeekEnum.Thursday,
                DayOfWeek.Friday => DayOfWeekEnum.Friday,
                _ => DayOfWeekEnum.Sunday
            };

            var scheduleTask = Task.Run(async () =>
            {
                if (activeSemester == null) return new List<ScheduleSlotData>();
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var data = await db.ScheduleSlots
                    .AsNoTracking()
                    .Where(s => s.Day == currentEnumDay && db.Enrollments.Any(e => e.StudentId == student.Id && e.SectionId == s.SectionId && e.Status == EnrollmentStatus.Enrolled && e.Section!.Course!.SemesterId == activeSemester.Id))
                    .OrderBy(s => s.StartTime)
                    .Select(s => new
                    {
                        CourseId = s.Section != null ? s.Section.CourseId : null,
                        CourseTitle = s.Section != null && s.Section.Course != null ? s.Section.Course.Title : null,
                        CourseCode = s.Section != null && s.Section.Course != null ? s.Section.Course.CourseCode : null,
                        ClassType = s.Section != null ? (ClassType?)s.Section.ClassType : null,
                        s.StartTime,
                        s.EndTime,
                        RoomName = s.Room != null ? s.Room.Name : null,
                        s.RoomId,
                        InstructorName = s.Section != null && s.Section.Instructor != null && s.Section.Instructor.User != null ? s.Section.Instructor.User.Name : null
                    })
                    .ToListAsync();

                return data.Select(s => new ScheduleSlotData
                {
                    CourseId = s.CourseId,
                    CourseTitle = s.CourseTitle,
                    CourseCode = s.CourseCode,
                    ClassType = s.ClassType,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    RoomName = s.RoomName,
                    RoomId = s.RoomId,
                    InstructorName = s.InstructorName
                }).ToList();
            });

            await Task.WhenAll(countsTask, enrollStatsTask, examsTask, scheduleTask);

            var counts = await countsTask;
            var enrollStats = await enrollStatsTask;
            var examsResult = await examsTask;
            var todayScheduleRaw = await scheduleTask;

            var openComplaints = counts?.OpenComplaints ?? 0;
            var unreadCount = counts?.UnreadNotifications ?? 0;
            var attendedCount = counts?.AttendedCount ?? 0;
            var totalCompletedSessions = counts?.TotalCompletedSessions ?? 0;

            var coursesEnrolledCount = enrollStats.Count;
            var currentCredits = enrollStats.CreditHours;

            var upcomingExamsCount = examsResult.Count;
            var nextExamDate = examsResult.NextDate;
            var nextExamDays = nextExamDate.HasValue ? (nextExamDate.Value - now).Days : (int?)null;

            var todaySchedule = todayScheduleRaw.Select(s => new
            {
                course_id = s.CourseId,
                course_name = s.CourseTitle,
                course_code = s.CourseCode,
                type = s.ClassType.HasValue ? s.ClassType.Value.ToString().ToLower() : "lecture",
                start_time = s.StartTime,
                end_time = s.EndTime,
                room = s.RoomName ?? s.RoomId,
                instructor = s.InstructorName ?? ""
            }).ToList();

            var currentWeek = activeSemester != null ? (now - activeSemester.StartDate).Days / 7 + 1 : 1;
            if (activeSemester != null && currentWeek > activeSemester.TotalWeeks) currentWeek = activeSemester.TotalWeeks;
            if (currentWeek < 1) currentWeek = 1;

            var registrationOpen = nextSemester != null && nextSemester.RegistrationStartDate <= now && nextSemester.RegistrationEndDate >= now;

            double attendanceAvgPct = 0.0;
            if (totalCompletedSessions > 0)
            {
                attendanceAvgPct = (double)attendedCount / totalCompletedSessions;
            }

            var response = new
            {
                student = new
                {
                    name = student.User?.Name,
                    student_id = student.AcademicNumber,
                    faculty = student.User?.Faculty?.Name ?? student.User?.Faculty?.NameAr,
                    department = student.User?.Department?.Name ?? student.User?.Department?.NameAr,
                    year = student.Level,
                    enrollment_status = student.User?.Status.ToString().ToLower()
                },
                semester = activeSemester != null ? new
                {
                    id = activeSemester.Id,
                    name = activeSemester.Name,
                    current_week = currentWeek,
                    total_weeks = activeSemester.TotalWeeks
                } : null,
                kpis = new
                {
                    gpa = student.GPA,
                    gpa_change = 0, 
                    attendance_avg_pct = attendanceAvgPct,
                    courses_enrolled = coursesEnrolledCount,
                    credit_hours_enrolled = currentCredits,
                    upcoming_exams_count = upcomingExamsCount,
                    next_exam_days = nextExamDays,
                    unread_count = unreadCount,
                    open_complaints = openComplaints
                },
                registration_window = nextSemester != null ? new
                {
                    is_open = registrationOpen,
                    semester_name = nextSemester.Name,
                    closes_in_days = (registrationOpen && nextSemester.RegistrationEndDate.HasValue) ? (nextSemester.RegistrationEndDate.Value - now).Days : (int?)null,
                    end_date = nextSemester.RegistrationEndDate
                } : null,
                today_schedule = todaySchedule
            };

            // Cache for 15 seconds
            _cache.Set(cacheKey, response, TimeSpan.FromSeconds(15));

            return Ok(response);
        }

        private class ScheduleSlotData
        {
            public string? CourseId { get; set; }
            public string? CourseTitle { get; set; }
            public string? CourseCode { get; set; }
            public ClassType? ClassType { get; set; }
            public string StartTime { get; set; } = string.Empty;
            public string EndTime { get; set; } = string.Empty;
            public string? RoomName { get; set; }
            public string RoomId { get; set; } = string.Empty;
            public string? InstructorName { get; set; }
        }
    }
}
