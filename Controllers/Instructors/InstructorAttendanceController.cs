using IbnElgm3a.Models;
using IbnElgm3a.Models.Data;
using IbnElgm3a.Services;
using IbnElgm3a.Services.Localization;
using IbnElgm3a.Attributes;
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
            var instructor = await _context.Instructors.AsNoTracking().FirstOrDefaultAsync(i => i.UserId == userId);
            if (instructor == null) return Unauthorized();

            var isTeaching = await _context.Sections.AsNoTracking().AnyAsync(s => s.CourseId == course_id && s.InstructorId == instructor.Id);
            if (!isTeaching) return Forbid();

            var sessions = await _context.Sessions
                .AsNoTracking()
                .Where(s => s.Section!.CourseId == course_id)
                .OrderBy(s => s.Date)
                .ToListAsync();

            var sessionIds = sessions.Select(s => s.Id).ToList();
            var attendanceRecords = await _context.AttendanceRecords
                .AsNoTracking()
                .Where(a => sessionIds.Contains(a.SessionId))
                .ToListAsync();

            var students = await _context.Enrollments
                .AsNoTracking()
                .Where(e => e.Section!.CourseId == course_id && e.Status == Enums.EnrollmentStatus.Enrolled)
                .Select(e => e.Student)
                .ToListAsync();

            var completedSessions = sessions.Where(s => s.AttendanceStatus == "completed").ToList();
            var completedSessionIds = completedSessions.Select(s => s.Id).ToList();

            var totalEnrolled = students.Count;
            double avgAttendanceRate = 1.0;
            int atRiskCount = 0;

            if (completedSessions.Any() && totalEnrolled > 0)
            {
                var totalPossibleSlots = completedSessions.Count * totalEnrolled;
                var totalPresentOrLate = attendanceRecords.Count(a => completedSessionIds.Contains(a.SessionId) && (a.Status == "present" || a.Status == "late"));
                avgAttendanceRate = (double)totalPresentOrLate / totalPossibleSlots;

                foreach (var student in students)
                {
                    var studentPresentOrLate = attendanceRecords.Count(a => a.StudentId == student!.Id && completedSessionIds.Contains(a.SessionId) && (a.Status == "present" || a.Status == "late"));
                    var rate = (double)studentPresentOrLate / completedSessions.Count;
                    if (rate < 0.6)
                    {
                        atRiskCount++;
                    }
                }
            }

            var sessionSummaries = sessions.OrderByDescending(s => s.Date).Select(s =>
            {
                var isToday = s.Date.Date == DateTime.UtcNow.Date;
                var hasRecorded = s.AttendanceStatus == "completed";
                return new
                {
                    session_id = s.Id,
                    session_number = s.SessionNumber,
                    date = s.Date.ToString("yyyy-MM-dd"),
                    attendance_status = s.AttendanceStatus,
                    present_count = hasRecorded ? attendanceRecords.Count(a => a.SessionId == s.Id && a.Status == "present") : 0,
                    late_count = hasRecorded ? attendanceRecords.Count(a => a.SessionId == s.Id && a.Status == "late") : 0,
                    absent_count = hasRecorded ? attendanceRecords.Count(a => a.SessionId == s.Id && a.Status == "absent") : 0,
                    is_today = isToday
                };
            }).ToList();

            return Ok(new
            {
                course_id = course_id,
                average_attendance_rate = avgAttendanceRate,
                at_risk_count = atRiskCount,
                sessions = sessionSummaries
            });
        }

        [HttpGet("sessions/{session_id}/attendance")]
        public async Task<IActionResult> GetSessionAttendance(string session_id)
        {
            var userId = GetUserId();
            var instructor = await _context.Instructors.AsNoTracking().FirstOrDefaultAsync(i => i.UserId == userId);
            if (instructor == null) return Unauthorized();

            var session = await _context.Sessions.AsNoTracking().Include(s => s.Section).FirstOrDefaultAsync(s => s.Id == session_id);
            if (session == null) return NotFound();

            if (session.Section?.InstructorId != instructor.Id) return Forbid();

            var enrollments = await _context.Enrollments
                .AsNoTracking()
                .Where(e => e.SectionId == session.SectionId && e.Status == Enums.EnrollmentStatus.Enrolled)
                .Select(e => new
                {
                    StudentId = e.Student!.Id,
                    UserName = e.Student.User != null ? e.Student.User.Name : "",
                    AcademicNumber = e.Student.AcademicNumber
                })
                .ToListAsync();

            var existingRecords = await _context.AttendanceRecords
                .AsNoTracking()
                .Where(a => a.SessionId == session_id)
                .ToDictionaryAsync(a => a.StudentId, a => a.Status);

            var roster = enrollments.Select(e =>
            {
                var initials = e.UserName.Length > 0
                    ? e.UserName.Substring(0, 1) + (e.UserName.Contains(' ') ? e.UserName.Split(' ')[1].Substring(0, 1) : "")
                    : "";
                var status = existingRecords.ContainsKey(e.StudentId) ? existingRecords[e.StudentId] : null;

                return new
                {
                    student_id = e.StudentId,
                    full_name = e.UserName,
                    initials = initials,
                    student_number = e.AcademicNumber,
                    status = status
                };
            }).OrderBy(e => e.full_name).ToList();

            var course = await _context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == session.Section!.CourseId);

            var presentCount = roster.Count(r => r.status == "present");
            var lateCount = roster.Count(r => r.status == "late");
            var absentCount = roster.Count(r => r.status == "absent");
            var excusedCount = roster.Count(r => r.status == "excused");
            var unrecordedCount = roster.Count(r => r.status == null);

            var responseObj = new
            {
                session = new
                {
                    id = session.Id,
                    course_code = course?.CourseCode ?? "",
                    course_name = course?.Title ?? "",
                    session_number = session.SessionNumber,
                    date = session.Date.ToString("yyyy-MM-dd"),
                    start_time = session.StartTime,
                    end_time = session.EndTime,
                    room = session.RoomName,
                    attendance_status = session.AttendanceStatus
                },
                records = roster,
                summary = new
                {
                    present_count = presentCount,
                    late_count = lateCount,
                    absent_count = absentCount,
                    excused_count = excusedCount,
                    unrecorded_count = unrecordedCount
                }
            };

            return Ok(responseObj);
        }

        [HttpPut("sessions/{session_id}/attendance")]
        public async Task<IActionResult> UpdateAttendance(string session_id, [FromBody] AttendanceUpdateRequest request)
        {
            var userId = GetUserId();
            var instructor = await _context.Instructors.AsNoTracking().FirstOrDefaultAsync(i => i.UserId == userId);
            if (instructor == null) return Unauthorized();

            var session = await _context.Sessions.Include(s => s.Section).FirstOrDefaultAsync(s => s.Id == session_id);
            if (session == null) return NotFound();

            if (session.Section?.InstructorId != instructor.Id) return Forbid();

            // Constraint: Cannot modify a completed attendance record
            if (session.AttendanceStatus == "completed")
            {
                return StatusCode(422, ApiResponse<object>.CreateError("SESSION_COMPLETED", "Cannot modify a completed attendance record."));
            }

            var existingRecords = await _context.AttendanceRecords
                .Where(a => a.SessionId == session_id)
                .ToListAsync();

            foreach (var item in request.Records)
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

            session.AttendanceStatus = request.Status;
            await _context.SaveChangesAsync();

            var allRecords = await _context.AttendanceRecords
                .Where(a => a.SessionId == session_id)
                .ToListAsync();

            var presentCount = allRecords.Count(r => r.Status == "present");
            var lateCount = allRecords.Count(r => r.Status == "late");
            var absentCount = allRecords.Count(r => r.Status == "absent");
            var excusedCount = allRecords.Count(r => r.Status == "excused");

            var responseObj = new
            {
                session_id = session_id,
                attendance_status = session.AttendanceStatus,
                summary = new
                {
                    present_count = presentCount,
                    late_count = lateCount,
                    absent_count = absentCount,
                    excused_count = excusedCount
                }
            };

            return Ok(responseObj);
        }

        [HttpPost("sessions/{session_id}/qr/generate")]
        public async Task<IActionResult> GenerateQr(string session_id)
        {
            var userId = GetUserId();
            var instructor = await _context.Instructors.AsNoTracking().FirstOrDefaultAsync(i => i.UserId == userId);
            if (instructor == null) return Unauthorized();

            var session = await _context.Sessions.Include(s => s.Section).FirstOrDefaultAsync(s => s.Id == session_id);
            if (session == null) return NotFound();

            if (session.Section?.InstructorId != instructor.Id) return Forbid();

            session.QrToken = Guid.NewGuid().ToString("N");
            session.QrExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5);
            session.IsQrActive = true;
            await _context.SaveChangesAsync();

            var responseObj = new
            {
                token = session.QrToken,
                qr_payload = session.QrToken,
                expires_at = session.QrExpiresAt,
                expires_in_seconds = 300
            };

            return StatusCode(201, responseObj);
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
                .AsNoTracking()
                .Where(a => a.SessionId == session_id && a.Status == "present")
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new
                {
                    student_id = a.StudentId,
                    FullName = a.Student != null && a.Student.User != null ? a.Student.User.Name : "",
                    checked_in_at = a.CreatedAt
                })
                .ToListAsync();

            // Build initials in memory (Substring/Split are not EF-translatable with optional args)
            var checkinsList = checkins.Select(a => new
            {
                student_id = a.student_id,
                full_name = a.FullName,
                initials = a.FullName.Length > 0
                    ? a.FullName[0].ToString() + (a.FullName.Contains(' ') ? a.FullName.Split(' ')[1][0].ToString() : "")
                    : "",
                checked_in_at = a.checked_in_at
            }).ToList();

            var totalEnrolled = await _context.Enrollments
                .AsNoTracking()
                .CountAsync(e => e.SectionId == session.SectionId && e.Status == Enums.EnrollmentStatus.Enrolled);

            var responseObj = new
            {
                checked_in_count = checkinsList.Count,
                total_enrolled = totalEnrolled,
                checkins = checkinsList
            };

            return Ok(responseObj);
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
            session.AttendanceStatus = "completed";

            // Find all enrolled students
            var enrollments = await _context.Enrollments
                .Where(e => e.SectionId == session.SectionId && e.Status == Enums.EnrollmentStatus.Enrolled)
                .ToListAsync();

            var existingRecords = await _context.AttendanceRecords
                .Where(a => a.SessionId == session_id)
                .ToListAsync();

            foreach (var enrollment in enrollments)
            {
                var record = existingRecords.FirstOrDefault(r => r.StudentId == enrollment.StudentId);
                if (record == null)
                {
                    // Student did not check in, mark absent
                    _context.AttendanceRecords.Add(new AttendanceRecord
                    {
                        SessionId = session_id,
                        StudentId = enrollment.StudentId,
                        Status = "absent"
                    });
                }
                else
                {
                    if (record.Status != "present" && record.Status != "late" && record.Status != "excused")
                    {
                        record.Status = "present";
                    }
                }
            }

            await _context.SaveChangesAsync();

            var allRecords = await _context.AttendanceRecords
                .Where(a => a.SessionId == session_id)
                .ToListAsync();

            var presentCount = allRecords.Count(r => r.Status == "present");
            var lateCount = allRecords.Count(r => r.Status == "late");
            var absentCount = allRecords.Count(r => r.Status == "absent");
            var excusedCount = allRecords.Count(r => r.Status == "excused");

            var responseObj = new
            {
                session_id = session_id,
                attendance_status = session.AttendanceStatus,
                summary = new
                {
                    present_count = presentCount,
                    late_count = lateCount,
                    absent_count = absentCount,
                    excused_count = excusedCount
                }
            };

            return Ok(responseObj);
        }
    }
}
