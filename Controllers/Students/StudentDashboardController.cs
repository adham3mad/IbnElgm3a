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

        public StudentDashboardController(AppDbContext context, INotificationService notificationService, IbnElgm3a.Services.Localization.ILocalizationService localizer)
        {
            _context = context;
            _notificationService = notificationService;
            _localizer = localizer;
        }

        private string GetUserId() => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";

        [HttpGet]
        [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Client)]
        public async Task<IActionResult> GetDashboard()
        {
            var userId = GetUserId();
            var student = await _context.Students
                .Include(s => s.User)
                    .ThenInclude(u => u!.Faculty)
                .Include(s => s.User)
                    .ThenInclude(u => u!.Department)
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student == null) return Unauthorized(new { message = _localizer.GetMessage("UNAUTHORIZED") });

            // Semester
            var activeSemester = await _context.Semesters.OrderByDescending(s => s.StartDate).FirstOrDefaultAsync();

            // Next semester for registration
            var now = DateTimeOffset.UtcNow;
            var nextSemester = await _context.Semesters
                .Where(s => s.StartDate > now)
                .OrderBy(s => s.StartDate)
                .FirstOrDefaultAsync();

            // Kpis
            var openComplaints = await _context.Complaints
                .CountAsync(c => c.StudentId == student.Id && c.Status != ComplaintStatus.Resolved && c.Status != ComplaintStatus.Closed);
            
            var unreadCount = await _notificationService.GetUnreadCountAsync(student.Id);

            var enrollments = await _context.Enrollments
                .Include(e => e.Section)
                    .ThenInclude(s => s!.Course)
                .Include(e => e.Section)
                    .ThenInclude(s => s!.ScheduleSlots)
                .Where(e => e.StudentId == student.Id && e.Section != null && e.Section.Course != null && e.Section.Course.SemesterId == activeSemester.Id && e.Status == EnrollmentStatus.Enrolled)
                .ToListAsync();

            var currentCredits = enrollments.Sum(e => e.Section?.Course?.CreditHours ?? 0);

            // Exams
            var enrolledCourseIds = enrollments.Select(en => en.Section?.CourseId).Where(id => id != null).ToList();
            var upcomingExams = await _context.Exams
                .Include(e => e.Course)
                .Where(e => e.Date >= now && enrolledCourseIds.Contains(e.CourseId))
                .OrderBy(e => e.Date)
                .ToListAsync();

            var nextExamDays = upcomingExams.Any() ? (upcomingExams.First().Date - now).Days : (int?)null;

            // Schedule for today
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

            var sectionIds = enrollments.Select(e => e.SectionId).ToList();
            var todaySlots = await _context.ScheduleSlots
                .Include(s => s.Section)
                    .ThenInclude(sec => sec!.Course)
                .Include(s => s.Section)
                    .ThenInclude(sec => sec!.Instructor)
                        .ThenInclude(i => i!.User)
                .Include(s => s.Room)
                .Where(s => sectionIds.Contains(s.SectionId) && s.Day == currentEnumDay)
                .OrderBy(s => s.StartTime)
                .ToListAsync();

            var currentWeek = activeSemester != null ? (now - activeSemester.StartDate).Days / 7 + 1 : 1;
            if (activeSemester != null && currentWeek > activeSemester.TotalWeeks) currentWeek = activeSemester.TotalWeeks;
            if (currentWeek < 1) currentWeek = 1;

            var registrationOpen = nextSemester != null && nextSemester.RegistrationStartDate <= now && nextSemester.RegistrationEndDate >= now;

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
                    attendance_avg_pct = activeSemester != null ? await _context.AttendanceRecords
                        .Where(a => a.StudentId == student.Id && a.Session!.Section!.Course!.SemesterId == activeSemester.Id && a.Session.AttendanceStatus == "completed")
                        .GroupBy(a => a.StudentId)
                        .Select(g => (double)g.Count(a => a.Status == "present" || a.Status == "late") / _context.Sessions.Count(s => s.Section!.Course!.SemesterId == activeSemester.Id && s.AttendanceStatus == "completed" && _context.Enrollments.Any(e => e.StudentId == student.Id && e.SectionId == s.SectionId)))
                        .FirstOrDefaultAsync() : 0.0,
                    courses_enrolled = enrollments.Count,
                    credit_hours_enrolled = currentCredits,
                    upcoming_exams_count = upcomingExams.Count,
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
                today_schedule = todaySlots.Select(s => new
                {
                    course_id = s.Section?.CourseId,
                    course_name = s.Section?.Course?.Title,
                    course_code = s.Section?.Course?.CourseCode,
                    type = s.Section?.ClassType.ToString().ToLower() ?? "lecture",
                    start_time = s.StartTime,
                    end_time = s.EndTime,
                    room = s.Room?.Name ?? s.RoomId,
                    instructor = s.Section?.Instructor?.User?.Name ?? ""
                }).ToList()
            };

            return Ok(response);
        }
    }
}
