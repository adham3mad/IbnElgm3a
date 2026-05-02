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
            int diff = (7 + (dt.DayOfWeek - DayOfWeek.Saturday)) % 7; // Saturday as first day of week as per mock JSON
            return dt.AddDays(-1 * diff).Date;
        }

        [HttpGet]
        [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Client)]
        public async Task<IActionResult> GetSchedule([FromQuery] string? week = null, [FromQuery] string? semester_id = null)
        {
            var userId = GetUserId();
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            if (student == null) return Unauthorized();

            var activeSemester = semester_id != null 
                ? await _context.Semesters.FindAsync(semester_id)
                : await _context.Semesters.OrderByDescending(s => s.StartDate).FirstOrDefaultAsync();

            if (activeSemester == null) return NotFound(new { message = _localizer.GetMessage("SEMESTER_NOT_FOUND") });

            var now = DateTimeOffset.UtcNow;
            
            DateTimeOffset weekStart;
            if (!string.IsNullOrEmpty(week) && week.Contains("-W"))
            {
                // mock parse, week is YYYY-Wxx
                var parts = week.Split("-W");
                if (parts.Length == 2 && int.TryParse(parts[0], out int y) && int.TryParse(parts[1], out int w))
                {
                    // rough calculation for ISO week to Date
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

            var enrollments = await _context.Enrollments
                .Where(e => e.StudentId == student.Id && e.Status == EnrollmentStatus.Enrolled)
                .Select(e => e.SectionId)
                .ToListAsync();

            var slots = await _context.ScheduleSlots
                .Include(s => s.Section)
                    .ThenInclude(sec => sec!.Course)
                .Include(s => s.Section)
                    .ThenInclude(sec => sec!.Instructor)
                        .ThenInclude(i => i!.User)
                .Include(s => s.Room)
                .Where(s => enrollments.Contains(s.SectionId))
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

                var daySlots = slots.Where(s => s.Day == currentEnumDay).OrderBy(s => s.StartTime).Select(s => new
                {
                    course_id = s.Section?.CourseId,
                    course_code = s.Section?.Course?.CourseCode,
                    course_name = s.Section?.Course?.Title,
                    type = s.Section?.ClassType.ToString().ToLower() ?? "lecture",
                    section = s.Section?.Name,
                    start_time = s.StartTime,
                    end_time = s.EndTime,
                    room = s.Room?.Name ?? s.RoomId,
                    instructor = s.Section?.Instructor?.User?.Name ?? "",
                    color = "#1a7090" // dummy static color per requirement
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

            // ISO Week Number
            var calISO = System.Globalization.CultureInfo.CurrentCulture.Calendar;
            var wN = calISO.GetWeekOfYear(weekStart.DateTime, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

            var response = new
            {
                week = $"{(weekStart.Year)}-W{wN:D2}",
                week_start = weekStart.ToString("yyyy-MM-dd"),
                week_end = weekStart.AddDays(6).ToString("yyyy-MM-dd"),
                days = daysResponse
            };

            return Ok(response);
        }
    }
}
