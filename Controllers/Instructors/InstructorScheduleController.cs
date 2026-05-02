using IbnElgm3a.Models;
using IbnElgm3a.Models.Data;
using IbnElgm3a.Services;
using IbnElgm3a.Services.Localization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using IbnElgm3a.DTOs.Schedules;

namespace IbnElgm3a.Controllers.Instructors
{
    [ApiController]
    [Route("instructor/Schedule")]
    [Authorize(Roles = "instructor")]
    public class InstructorScheduleController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILocalizationService _localizer;

        public InstructorScheduleController(AppDbContext context, ILocalizationService localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        [HttpGet("schedule")]
        public async Task<IActionResult> GetWeeklySchedule([FromQuery] string? week_start)
        {
            var userId = GetUserId();
            var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.UserId == userId);
            if (instructor == null) return Unauthorized();

            DateTime start;
            if (string.IsNullOrEmpty(week_start))
            {
                var today = DateTime.UtcNow.Date;
                int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
                start = today.AddDays(-1 * diff);
            }
            else
            {
                start = DateTime.Parse(week_start).Date;
            }

            var end = start.AddDays(7);
            var activeSemester = await _context.Semesters
                .Where(s => s.StartDate <= start && s.EndDate >= start)
                .FirstOrDefaultAsync() ?? await _context.Semesters.OrderByDescending(s => s.StartDate).FirstOrDefaultAsync();

            var sessions = await _context.Sessions
                .Include(s => s.Section)
                    .ThenInclude(sec => sec!.Course)
                .Where(s => s.Section!.InstructorId == instructor.Id && s.Date >= start && s.Date < end)
                .ToListAsync();

            var days = new List<object>();
            for (int i = 0; i < 7; i++)
            {
                var date = start.AddDays(i);
                var daySessions = sessions.Where(s => s.Date.Date == date.Date).OrderBy(s => s.StartTime).Select(s => new
                {
                    id = s.Id,
                    course_id = s.Section!.CourseId,
                    course_code = s.Section.Course!.CourseCode,
                    course_name = s.Section.Course.Title,
                    session_number = s.SessionNumber,
                    type = s.Type.ToString().ToLower(),
                    date = s.Date.ToString("yyyy-MM-dd"),
                    start_time = s.StartTime,
                    end_time = s.EndTime,
                    room = s.RoomName,
                    week_number = s.WeekNumber,
                    attendance_status = s.AttendanceStatus,
                    is_recurring = s.IsRecurring
                }).ToList();

                days.Add(new
                {
                    date = date.ToString("yyyy-MM-dd"),
                    day_name = date.DayOfWeek.ToString(),
                    day_abbr = date.DayOfWeek.ToString().Substring(0, 3),
                    is_today = date.Date == DateTime.UtcNow.Date,
                    sessions = daySessions
                });
            }

            return Ok(new
            {
                data = new
                {
                    week_start = start.ToString("yyyy-MM-dd"),
                    week_end = start.AddDays(6).ToString("yyyy-MM-dd"),
                    week_number = activeSemester != null ? (start - activeSemester.StartDate).Days / 7 + 1 : 1,
                    semester = activeSemester?.Name ?? "",
                    days = days
                }
            });
        }

        [HttpGet("sessions/{session_id}")]
        public async Task<IActionResult> GetSessionDetail(string session_id)
        {
            var userId = GetUserId();
            var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.UserId == userId);
            if (instructor == null) return Unauthorized();

            var session = await _context.Sessions
                .Include(s => s.Section)
                    .ThenInclude(sec => sec!.Course)
                .FirstOrDefaultAsync(s => s.Id == session_id);

            if (session == null) return NotFound();

            if (session.Section?.InstructorId != instructor.Id) return Forbid();

            var presentCount = await _context.AttendanceRecords.CountAsync(a => a.SessionId == session_id && a.Status == "present");
            var studentCount = await _context.Enrollments.CountAsync(e => e.SectionId == session.SectionId && e.Status == Enums.EnrollmentStatus.Enrolled);

            return Ok(new
            {
                data = new
                {
                    session = new
                    {
                        id = session.Id,
                        course_id = session.Section!.CourseId,
                        course_code = session.Section.Course!.CourseCode,
                        course_name = session.Section.Course.Title,
                        session_number = session.SessionNumber,
                        type = session.Type.ToString().ToLower(),
                        date = session.Date.ToString("yyyy-MM-dd"),
                        start_time = session.StartTime,
                        end_time = session.EndTime,
                        room = session.RoomName,
                        week_number = session.WeekNumber,
                        attendance_status = session.AttendanceStatus,
                        is_recurring = session.IsRecurring
                    },
                    attendance_summary = new
                    {
                        present_count = presentCount,
                        absent_count = studentCount - presentCount,
                        total_count = studentCount,
                        attendance_rate = studentCount > 0 ? (float)presentCount / studentCount : 0
                    }
                }
            });
        }

        [HttpGet("sessions/conflict-check")]
        public async Task<IActionResult> CheckConflict([FromQuery] string date, [FromQuery] string start_time, [FromQuery] string end_time, [FromQuery] string room)
        {
            var sessionDate = DateTime.Parse(date).Date;
            var conflict = await _context.Sessions
                .AnyAsync(s => s.Date.Date == sessionDate && s.RoomName == room &&
                               ((s.StartTime.CompareTo(start_time) >= 0 && s.StartTime.CompareTo(end_time) < 0) ||
                                (s.EndTime.CompareTo(start_time) > 0 && s.EndTime.CompareTo(end_time) <= 0)));

            return Ok(new
            {
                data = new
                {
                    has_conflict = conflict,
                    conflict_details = conflict ? _localizer.GetMessage("ROOM_CONFLICT") : null
                }
            });
        }

        [HttpPost("sessions")]
        public async Task<IActionResult> CreateSession([FromBody] CreateSessionRequest request)
        {
            var userId = GetUserId();
            var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.UserId == userId);
            if (instructor == null) return Unauthorized();

            var section = await _context.Sections.FindAsync(request.SectionId);
            if (section == null) return NotFound();

            if (section.InstructorId != instructor.Id) return Forbid();

            var session = new Session
            {
                SectionId = request.SectionId,
                Type = request.Type,
                Date = DateTime.Parse(request.Date).Date,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                RoomName = request.Room,
                Notes = request.Notes,
                SessionNumber = request.SessionNumber,
                WeekNumber = request.WeekNumber,
                AttendanceStatus = "pending",
                IsRecurring = false
            };

            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSessionDetail), new { session_id = session.Id }, new { data = session });
        }


    }
}
