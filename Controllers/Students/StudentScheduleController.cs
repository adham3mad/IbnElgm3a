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
    [Route("student/schedule")]
    [Authorize(Roles = "student")]
    public class StudentScheduleController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IbnElgm3a.Services.Localization.ILocalizationService _localizer;

        public StudentScheduleController(AppDbContext context, IbnElgm3a.Services.Localization.ILocalizationService localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        private string GetUserId() => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";

        private DateTimeOffset GetStartOfWeek(DateTimeOffset dt)
        {
            int diff = (7 + (dt.DayOfWeek - DayOfWeek.Saturday)) % 7;
            return dt.AddDays(-1 * diff).Date;
        }

        [HttpGet]
        [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Client)]
        public async Task<IActionResult> GetSchedule([FromQuery] string? week = null, [FromQuery] string? semester_id = null)
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
            var activeSemesterId = semester_id
                ?? await _context.Semesters
                    .AsNoTracking()
                    .OrderByDescending(s => s.StartDate)
                    .Select(s => s.Id)
                    .FirstOrDefaultAsync();

            if (activeSemesterId == null) return NotFound(new { message = _localizer.GetMessage("SEMESTER_NOT_FOUND") });

            DateTimeOffset weekStart;
            if (!string.IsNullOrEmpty(week) && week.Contains("-W"))
            {
                var parts = week.Split("-W");
                if (parts.Length == 2 && int.TryParse(parts[0], out int y) && int.TryParse(parts[1], out int w))
                {
                    DateTime jan1 = new DateTime(y, 1, 1);
                    int daysOffset = DayOfWeek.Thursday - jan1.DayOfWeek;
                    DateTime firstThursday = jan1.AddDays(daysOffset);
                    var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
                    int firstWeek = cal.GetWeekOfYear(firstThursday, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
                    var weekNum = firstWeek <= 1 ? firstWeek : 1;
                    DateTime result = firstThursday.AddDays((w - weekNum) * 7);
                    weekStart = GetStartOfWeek(new DateTimeOffset(result));
                }
                else
                {
                    weekStart = GetStartOfWeek(now);
                }
            }
            else
            {
                weekStart = GetStartOfWeek(now);
            }

            // Fetch enrolled section IDs (fast, lightweight query)
            var enrolledSectionIds = await _context.Enrollments
                .AsNoTracking()
                .Where(e => e.StudentId == studentId && e.Status == EnrollmentStatus.Enrolled)
                .Select(e => e.SectionId)
                .ToListAsync();

            // Fetch schedule slots with projection (no Include)
            var slots = await _context.ScheduleSlots
                .AsNoTracking()
                .Where(s => enrolledSectionIds.Contains(s.SectionId))
                .Select(s => new
                {
                    Day = s.Day,
                    CourseId = s.Section != null ? s.Section.CourseId : null,
                    CourseCode = s.Section != null && s.Section.Course != null ? s.Section.Course.CourseCode : null,
                    CourseName = s.Section != null && s.Section.Course != null ? s.Section.Course.Title : null,
                    ClassType = s.Section != null ? (ClassType?)s.Section.ClassType : null,
                    SectionName = s.Section != null ? s.Section.Name : null,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    RoomName = s.Room != null ? s.Room.Name : s.RoomId,
                    InstructorName = s.Section != null && s.Section.Instructor != null && s.Section.Instructor.User != null
                        ? s.Section.Instructor.User.Name : null
                })
                .ToListAsync();

            var daysResponse = new List<object>();

            for (int i = 0; i < 7; i++)
            {
                var currentDay = weekStart.AddDays(i);
                var dayOfWeek = currentDay.DayOfWeek;
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

                var daySlots = slots
                    .Where(s => s.Day == currentEnumDay)
                    .OrderBy(s => s.StartTime)
                    .Select(s => new
                    {
                        course_id = s.CourseId,
                        course_code = s.CourseCode,
                        course_name = s.CourseName,
                        type = s.ClassType.HasValue ? s.ClassType.Value.ToString().ToLower() : "lecture",
                        section = s.SectionName,
                        start_time = s.StartTime,
                        end_time = s.EndTime,
                        room = s.RoomName,
                        instructor = s.InstructorName ?? "",
                        color = "#1a7090"
                    }).ToList();

                if (daySlots.Any())
                {
                    daysResponse.Add(new
                    {
                        day = dayOfWeek.ToString(),
                        date = currentDay.ToString("yyyy-MM-dd"),
                        slots = daySlots
                    });
                }
            }

            var calISO = System.Globalization.CultureInfo.CurrentCulture.Calendar;
            var wN = calISO.GetWeekOfYear(weekStart.DateTime, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

            var response = new
            {
                week = $"{weekStart.Year}-W{wN:D2}",
                week_start = weekStart.ToString("yyyy-MM-dd"),
                week_end = weekStart.AddDays(6).ToString("yyyy-MM-dd"),
                days = daysResponse
            };

            return Ok(response);
        }
    }
}
