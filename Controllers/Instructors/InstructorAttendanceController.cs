using IbnElgm3a.Models;
using IbnElgm3a.Models.Data;
using IbnElgm3a.Services;
using IbnElgm3a.Services.Localization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using IbnElgm3a.DTOs.Academics;

namespace IbnElgm3a.Controllers.Instructors
{
    [ApiController]
    [Route("instructor/attendance")]
    [Authorize(Roles = "instructor")]
    public class InstructorAttendanceController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILocalizationService _localizer;

        public InstructorAttendanceController(AppDbContext context, ILocalizationService localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        [HttpGet("courses/{course_id}/attendance")]
        public async Task<IActionResult> GetCourseAttendance(string course_id)
        {
            var userId = GetUserId();
            var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.UserId == userId);
            if (instructor == null) return Unauthorized();

            var isTeaching = await _context.Sections.AnyAsync(s => s.CourseId == course_id && s.InstructorId == instructor.Id);
            if (!isTeaching) return Forbid();

            var sessions = await _context.Sessions
                .Where(s => s.Section!.CourseId == course_id)
                .OrderBy(s => s.Date)
                .ToListAsync();

            var sessionIds = sessions.Select(s => s.Id).ToList();
            var attendanceRecords = await _context.AttendanceRecords
                .Where(a => sessionIds.Contains(a.SessionId))
                .ToListAsync();

            var students = await _context.Enrollments
                .Include(e => e.Student)
                    .ThenInclude(s => s!.User)
                .Where(e => e.Section!.CourseId == course_id && e.Status == Enums.EnrollmentStatus.Enrolled)
                .Select(e => e.Student)
                .ToListAsync();

            var studentAttendance = students.Select(s => new
            {
                student_id = s!.Id,
                full_name = s.User!.Name,
                student_number = s.AcademicNumber,
                present_count = attendanceRecords.Count(a => a.StudentId == s.Id && a.Status == "present"),
                late_count = attendanceRecords.Count(a => a.StudentId == s.Id && a.Status == "late"),
                absent_count = attendanceRecords.Count(a => a.StudentId == s.Id && a.Status == "absent"),
                excused_count = attendanceRecords.Count(a => a.StudentId == s.Id && a.Status == "excused"),
                total_sessions = sessions.Count(sess => sess.AttendanceStatus == "completed")
            }).ToList();

            return Ok(new
            {
                data = new
                {
                    total_sessions = sessions.Count(s => s.AttendanceStatus == "completed"),
                    students = studentAttendance
                }
            });
        }

        [HttpGet("sessions/{session_id}/attendance")]
        public async Task<IActionResult> GetSessionAttendance(string session_id)
        {
            var userId = GetUserId();
            var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.UserId == userId);
            if (instructor == null) return Unauthorized();

            var session = await _context.Sessions.Include(s => s.Section).FirstOrDefaultAsync(s => s.Id == session_id);
            if (session == null) return NotFound();

            if (session.Section?.InstructorId != instructor.Id) return Forbid();

            var enrollments = await _context.Enrollments
                .Include(e => e.Student)
                    .ThenInclude(s => s!.User)
                .Where(e => e.SectionId == session.SectionId && e.Status == Enums.EnrollmentStatus.Enrolled)
                .ToListAsync();

            var existingRecords = await _context.AttendanceRecords
                .Where(a => a.SessionId == session_id)
                .ToDictionaryAsync(a => a.StudentId, a => a.Status);

            var roster = enrollments.Select(e => new
            {
                student_id = e.StudentId,
                full_name = e.Student!.User!.Name,
                student_number = e.Student.AcademicNumber,
                status = existingRecords.ContainsKey(e.StudentId) ? existingRecords[e.StudentId] : "absent"
            }).OrderBy(e => e.full_name).ToList();

            return Ok(new { data = roster });
        }

        [HttpPut("sessions/{session_id}/attendance")]
        public async Task<IActionResult> UpdateAttendance(string session_id, [FromBody] List<AttendanceUpdateItem> records)
        {
            var userId = GetUserId();
            var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.UserId == userId);
            if (instructor == null) return Unauthorized();

            var session = await _context.Sessions.Include(s => s.Section).FirstOrDefaultAsync(s => s.Id == session_id);
            if (session == null) return NotFound();

            if (session.Section?.InstructorId != instructor.Id) return Forbid();

            var existingRecords = await _context.AttendanceRecords
                .Where(a => a.SessionId == session_id)
                .ToListAsync();

            foreach (var item in records)
            {
                var record = existingRecords.FirstOrDefault(r => r.StudentId == item.StudentId);
                if (record != null)
                {
                    record.Status = item.Status;
                }
                else
                {
                    _context.AttendanceRecords.Add(new AttendanceRecord
                    {
                        SessionId = session_id,
                        StudentId = item.StudentId,
                        Status = item.Status
                    });
                }
            }

            session.AttendanceStatus = "completed";
            await _context.SaveChangesAsync();

            return Ok(new { data = new { message = _localizer.GetMessage("ATTENDANCE_SAVED") } });
        }

        [HttpPost("sessions/{session_id}/qr/generate")]
        public async Task<IActionResult> GenerateQr(string session_id)
        {
            var userId = GetUserId();
            var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.UserId == userId);
            if (instructor == null) return Unauthorized();

            var session = await _context.Sessions.Include(s => s.Section).FirstOrDefaultAsync(s => s.Id == session_id);
            if (session == null) return NotFound();

            if (session.Section?.InstructorId != instructor.Id) return Forbid();

            session.QrToken = Guid.NewGuid().ToString("N");
            session.QrExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5);
            session.IsQrActive = true;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                data = new
                {
                    qr_token = session.QrToken,
                    expires_at = session.QrExpiresAt
                }
            });
        }

        [HttpGet("sessions/{session_id}/qr/checkins")]
        public async Task<IActionResult> GetQrCheckins(string session_id)
        {
            var userId = GetUserId();
            var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.UserId == userId);
            if (instructor == null) return Unauthorized();

            var session = await _context.Sessions.Include(s => s.Section).FirstOrDefaultAsync(s => s.Id == session_id);
            if (session == null) return NotFound();

            if (session.Section?.InstructorId != instructor.Id) return Forbid();

            var checkins = await _context.AttendanceRecords
                .Include(a => a.Student)
                    .ThenInclude(s => s!.User)
                .Where(a => a.SessionId == session_id && a.Status == "present")
                .Select(a => new
                {
                    student_id = a.StudentId,
                    full_name = a.Student!.User!.Name,
                    checkin_time = a.CreatedAt
                })
                .ToListAsync();

            return Ok(new { data = checkins });
        }

        [HttpPatch("sessions/{session_id}/qr/close")]
        public async Task<IActionResult> CloseQr(string session_id)
        {
            var userId = GetUserId();
            var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.UserId == userId);
            if (instructor == null) return Unauthorized();

            var session = await _context.Sessions.Include(s => s.Section).FirstOrDefaultAsync(s => s.Id == session_id);
            if (session == null) return NotFound();

            if (session.Section?.InstructorId != instructor.Id) return Forbid();

            session.IsQrActive = false;
            await _context.SaveChangesAsync();

            return Ok(new { data = new { message = _localizer.GetMessage("QR_CHECKIN_CLOSED") } });
        }


    }
}
