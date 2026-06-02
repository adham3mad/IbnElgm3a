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
        private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _cache;

        public StudentDashboardController(
            AppDbContext context, 
            INotificationService notificationService, 
            IbnElgm3a.Services.Localization.ILocalizationService localizer,
            Microsoft.Extensions.Caching.Memory.IMemoryCache cache)
        {
            _context = context;
            _notificationService = notificationService;
            _localizer = localizer;
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

            // 1. Fetch semesters list to identify active/next semester (fast, simple query)
            var semesters = await _context.Semesters
                .AsNoTracking()
                .OrderByDescending(s => s.StartDate)
                .ToListAsync();

            var activeSemester = semesters.FirstOrDefault();
            var nextSemester = semesters
                .Where(s => s.StartDate > now)
                .OrderBy(s => s.StartDate)
                .FirstOrDefault();

            var activeSemesterId = activeSemester?.Id;
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

            // 2. Fetch the student profile, counts, KPIs, and today's schedule in a single split query.
            // This prevents parallel DB connection overhead and excludes unused/encrypted fields from being fetched.
            var data = await _context.Students
                .AsNoTracking()
                .Where(s => s.UserId == userId)
                .Select(s => new
                {
                    Student = s,
                    UserName = s.User != null ? s.User.Name : null,
                    UserStatus = s.User != null ? (UserStatus?)s.User.Status : null,
                    FacultyName = s.User != null && s.User.Faculty != null ? s.User.Faculty.Name : null,
                    FacultyNameAr = s.User != null && s.User.Faculty != null ? s.User.Faculty.NameAr : null,
                    DepartmentName = s.User != null && s.User.Department != null ? s.User.Department.Name : null,
                    DepartmentNameAr = s.User != null && s.User.Department != null ? s.User.Department.NameAr : null,

                    OpenComplaints = _context.Complaints.Count(c => c.StudentId == userId && c.Status != ComplaintStatus.Resolved && c.Status != ComplaintStatus.Closed),
                    
                    UnreadNotifications = _context.Notifications.Count(n => n.StudentId == s.Id && !n.IsRead),
                    
                    AttendedCount = activeSemesterId != null 
                        ? _context.AttendanceRecords.Count(a => a.StudentId == s.Id && a.Session!.Section!.Course!.SemesterId == activeSemesterId && a.Session.AttendanceStatus == "completed" && (a.Status == "present" || a.Status == "late")) 
                        : 0,
                        
                    TotalCompletedSessions = activeSemesterId != null 
                        ? _context.Sessions.Count(sess => sess.AttendanceStatus == "completed" && _context.Enrollments.Any(e => e.StudentId == s.Id && e.SectionId == sess.SectionId && e.Status == EnrollmentStatus.Enrolled && e.Section!.Course!.SemesterId == activeSemesterId)) 
                        : 0,

                    EnrollStats = activeSemesterId != null 
                        ? _context.Enrollments
                            .Where(e => e.StudentId == s.Id && e.Section != null && e.Section.Course != null && e.Section.Course.SemesterId == activeSemesterId && e.Status == EnrollmentStatus.Enrolled)
                            .GroupBy(e => 1)
                            .Select(g => new
                            {
                                Count = g.Count(),
                                CreditHours = g.Sum(e => e.Section!.Course!.CreditHours)
                            })
                            .FirstOrDefault()
                        : null,

                    UpcomingExamsCount = activeSemesterId != null
                        ? _context.Exams.Count(e => e.Date >= now && _context.Enrollments.Any(en => en.StudentId == s.Id && en.Section!.CourseId == e.CourseId && en.Status == EnrollmentStatus.Enrolled && en.Section!.Course!.SemesterId == activeSemesterId))
                        : 0,

                    NextExamDate = activeSemesterId != null
                        ? _context.Exams
                            .Where(e => e.Date >= now && _context.Enrollments.Any(en => en.StudentId == s.Id && en.Section!.CourseId == e.CourseId && en.Status == EnrollmentStatus.Enrolled && en.Section!.Course!.SemesterId == activeSemesterId))
                            .OrderBy(e => e.Date)
                            .Select(e => (DateTimeOffset?)e.Date)
                            .FirstOrDefault()
                        : null,

                    TodaySchedule = activeSemesterId != null
                        ? _context.ScheduleSlots
                            .Where(slot => slot.Day == currentEnumDay && _context.Enrollments.Any(e => e.StudentId == s.Id && e.SectionId == slot.SectionId && e.Status == EnrollmentStatus.Enrolled && e.Section!.Course!.SemesterId == activeSemesterId))
                            .OrderBy(slot => slot.StartTime)
                            .Select(slot => new ScheduleSlotData
                            {
                                CourseId = slot.Section != null ? slot.Section.CourseId : null,
                                CourseTitle = slot.Section != null && slot.Section.Course != null ? slot.Section.Course.Title : null,
                                CourseCode = slot.Section != null && slot.Section.Course != null ? slot.Section.Course.CourseCode : null,
                                ClassType = slot.Section != null ? (ClassType?)slot.Section.ClassType : null,
                                StartTime = slot.StartTime,
                                EndTime = slot.EndTime,
                                RoomName = slot.Room != null ? slot.Room.Name : null,
                                RoomId = slot.RoomId,
                                InstructorName = slot.Section != null && slot.Section.Instructor != null && slot.Section.Instructor.User != null ? slot.Section.Instructor.User.Name : null
                            })
                            .ToList()
                        : new List<ScheduleSlotData>()
                })
                .AsSplitQuery()
                .FirstOrDefaultAsync();

            if (data == null) return Unauthorized(new { message = _localizer.GetMessage("UNAUTHORIZED") });

            var student = data.Student;
            var enrollStats = data.EnrollStats;
            var openComplaints = data.OpenComplaints;
            var unreadCount = data.UnreadNotifications;
            var attendedCount = data.AttendedCount;
            var totalCompletedSessions = data.TotalCompletedSessions;
            var coursesEnrolledCount = enrollStats?.Count ?? 0;
            var currentCredits = enrollStats?.CreditHours ?? 0;
            var upcomingExamsCount = data.UpcomingExamsCount;
            var nextExamDate = data.NextExamDate;
            var nextExamDays = nextExamDate.HasValue ? (nextExamDate.Value - now).Days : (int?)null;

            var todaySchedule = data.TodaySchedule.Select(s => new
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
                    name = data.UserName,
                    student_id = student.AcademicNumber,
                    faculty = data.FacultyName ?? data.FacultyNameAr,
                    department = data.DepartmentName ?? data.DepartmentNameAr,
                    year = student.Level,
                    enrollment_status = data.UserStatus.HasValue ? data.UserStatus.Value.ToString().ToLower() : null
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
