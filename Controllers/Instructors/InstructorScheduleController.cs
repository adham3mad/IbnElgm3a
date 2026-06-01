using IbnElgm3a.Models;
using IbnElgm3a.Models.Data;
using IbnElgm3a.Services;
using IbnElgm3a.Services.Localization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using IbnElgm3a.DTOs.Schedules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IbnElgm3a.Controllers.Instructors
{
    [ApiController]
    [Route("instructor")]
    [Authorize(Roles = "instructor")]
    public class InstructorScheduleController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILocalizationService _localizer;
        private readonly INotificationService _notificationService;

        public InstructorScheduleController(AppDbContext context, ILocalizationService localizer, INotificationService notificationService)
        {
            _context = context;
            _localizer = localizer;
            _notificationService = notificationService;
        }

        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        [HttpGet("schedule")]
        public async Task<IActionResult> GetWeeklySchedule([FromQuery] string? week_start)
        {
            var userId = GetUserId();
            var instructor = await _context.Instructors.AsNoTracking().FirstOrDefaultAsync(i => i.UserId == userId);
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
            start = DateTime.SpecifyKind(start, DateTimeKind.Utc);
            var end = DateTime.SpecifyKind(start.AddDays(7), DateTimeKind.Utc);

            var activeSemester = await _context.Semesters
                .AsNoTracking()
                .Where(s => s.StartDate <= start && s.EndDate >= start)
                .FirstOrDefaultAsync() ?? await _context.Semesters.AsNoTracking().OrderByDescending(s => s.StartDate).FirstOrDefaultAsync();

            var sessions = await _context.Sessions
                .AsNoTracking()
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
                week_start = start.ToString("yyyy-MM-dd"),
                week_end = start.AddDays(6).ToString("yyyy-MM-dd"),
                week_number = activeSemester != null ? (start - activeSemester.StartDate).Days / 7 + 1 : 1,
                semester = activeSemester?.Name ?? "",
                days = days
            });
        }

        [HttpGet("sessions/{session_id}")]
        public async Task<IActionResult> GetSessionDetail(string session_id)
        {
            var userId = GetUserId();
            var instructor = await _context.Instructors.AsNoTracking().FirstOrDefaultAsync(i => i.UserId == userId);
            if (instructor == null) return Unauthorized();

            var session = await _context.Sessions
                .AsNoTracking()
                .Include(s => s.Section)
                    .ThenInclude(sec => sec!.Course)
                .FirstOrDefaultAsync(s => s.Id == session_id);

            if (session == null) return NotFound();

            if (session.Section?.InstructorId != instructor.Id) return Forbid();

            var previousSession = await _context.Sessions
                .AsNoTracking()
                .Where(s => s.SectionId == session.SectionId && s.Date < session.Date)
                .OrderByDescending(s => s.Date)
                .Select(s => new { session_id = s.Id, session_number = s.SessionNumber, date = s.Date.ToString("yyyy-MM-dd") })
                .FirstOrDefaultAsync();

            var nextSession = await _context.Sessions
                .AsNoTracking()
                .Where(s => s.SectionId == session.SectionId && s.Date > session.Date)
                .OrderBy(s => s.Date)
                .Select(s => new { session_id = s.Id, session_number = s.SessionNumber, date = s.Date.ToString("yyyy-MM-dd") })
                .FirstOrDefaultAsync();

            var attendanceRecords = await _context.AttendanceRecords
                .AsNoTracking()
                .Where(a => a.SessionId == session_id)
                .ToListAsync();

            var studentCount = await _context.Enrollments
                .AsNoTracking()
                .CountAsync(e => e.SectionId == session.SectionId && e.Status == Enums.EnrollmentStatus.Enrolled);

            var presentCount = attendanceRecords.Count(a => a.Status == "present");
            var lateCount = attendanceRecords.Count(a => a.Status == "late");
            var absentCount = attendanceRecords.Count(a => a.Status == "absent");
            var excusedCount = attendanceRecords.Count(a => a.Status == "excused");
            var notRecordedCount = studentCount - attendanceRecords.Count;

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
                    previous_session = previousSession,
                    next_session = nextSession,
                    attendance_summary = new
                    {
                        present_count = presentCount,
                        late_count = lateCount,
                        absent_count = absentCount,
                        excused_count = excusedCount,
                        not_recorded_count = notRecordedCount
                    },
                    session_notes = session.Notes,
                    week_label = $"Week {session.WeekNumber}"
                }
            });
        }

        [HttpGet("sessions/conflict-check")]
        public async Task<IActionResult> CheckConflict(
            [FromQuery] string course_id, 
            [FromQuery] string date, 
            [FromQuery] string start_time, 
            [FromQuery] string end_time, 
            [FromQuery] string? room = null,
            [FromQuery] string? exclude_session_id = null)
        {
            var userId = GetUserId();
            var instructor = await _context.Instructors.AsNoTracking().FirstOrDefaultAsync(i => i.UserId == userId);
            if (instructor == null) return Unauthorized();

            if (!DateTime.TryParse(date, out var parsedDate))
            {
                return BadRequest(new { error = "VALIDATION_ERROR", message = _localizer.GetMessage("INVALID_DATE_FORMAT") });
            }
            parsedDate = DateTime.SpecifyKind(parsedDate.Date, DateTimeKind.Utc);

            var conflicts = await GetConflictingSessionsAsync(
                courseId: course_id,
                instructorId: instructor.Id,
                date: parsedDate,
                startTime: start_time,
                endTime: end_time,
                room: room,
                excludeSessionId: exclude_session_id
            );

            return Ok(new
            {
                data = new
                {
                    has_conflict = conflicts.Any(),
                    conflicting_sessions = conflicts.Select(s => new
                    {
                        session_id = s.Id,
                        course_code = s.Section?.Course?.CourseCode ?? "",
                        course_name = s.Section?.Course?.Title ?? "",
                        date = s.Date.ToString("yyyy-MM-dd"),
                        start_time = s.StartTime,
                        end_time = s.EndTime,
                        room = s.RoomName
                    }).ToList()
                }
            });
        }

        [HttpPost("sessions")]
        public async Task<IActionResult> CreateSession([FromBody] CreateSessionRequest request)
        {
            var userId = GetUserId();
            var instructor = await _context.Instructors.AsNoTracking().FirstOrDefaultAsync(i => i.UserId == userId);
            if (instructor == null) return Unauthorized();

            var section = await _context.Sections
                .AsNoTracking()
                .Include(s => s.Course)
                .FirstOrDefaultAsync(s => s.CourseId == request.CourseId && s.InstructorId == instructor.Id);

            if (section == null)
            {
                return NotFound(new { error = new { code = "NOT_FOUND", message = _localizer.GetMessage("SECTION_NOT_FOUND") } });
            }

            if (!DateTime.TryParse(request.Date, out var sessionDate))
            {
                return BadRequest(new { error = "VALIDATION_ERROR", message = _localizer.GetMessage("INVALID_DATE_FORMAT") });
            }
            sessionDate = DateTime.SpecifyKind(sessionDate.Date, DateTimeKind.Utc);

            var semester = await _context.Semesters.AsNoTracking().FirstOrDefaultAsync(s => s.Id == section.SemesterId);
            if (semester == null)
            {
                return BadRequest(new { error = "NOT_FOUND", message = _localizer.GetMessage("SEMESTER_NOT_FOUND") });
            }

            var proposedDates = new List<DateTime> { sessionDate };
            if (request.IsRecurring)
            {
                var tempDate = sessionDate.AddDays(7);
                while (tempDate.Date <= semester.EndDate.Date)
                {
                    proposedDates.Add(DateTime.SpecifyKind(tempDate.Date, DateTimeKind.Utc));
                    tempDate = tempDate.AddDays(7);
                }
            }

            var allConflicts = new List<Session>();
            foreach (var date in proposedDates)
            {
                var conflicts = await GetConflictingSessionsAsync(
                    courseId: request.CourseId,
                    instructorId: instructor.Id,
                    date: date,
                    startTime: request.StartTime,
                    endTime: request.EndTime,
                    room: request.Room
                );
                allConflicts.AddRange(conflicts);
            }

            if (allConflicts.Any())
            {
                var uniqueConflicts = allConflicts.GroupBy(c => c.Id).Select(g => g.First()).ToList();
                return Conflict(new
                {
                    error = new
                    {
                        code = "SCHEDULE_CONFLICT",
                        message = _localizer.GetMessage("SCHEDULE_CONFLICT"),
                        details = new
                        {
                            conflicting_sessions = uniqueConflicts.Select(s => new
                            {
                                session_id = s.Id,
                                course_code = s.Section?.Course?.CourseCode ?? "",
                                course_name = s.Section?.Course?.Title ?? "",
                                date = s.Date.ToString("yyyy-MM-dd"),
                                start_time = s.StartTime,
                                end_time = s.EndTime,
                                room = s.RoomName
                            }).ToList()
                        }
                    }
                });
            }

            var nextSessionNumber = (await _context.Sessions
                .Where(s => s.SectionId == section.Id)
                .Select(s => (int?)s.SessionNumber)
                .MaxAsync()) ?? 0;
            nextSessionNumber++;

            var baseWeekNumber = Math.Max(1, (sessionDate - semester.StartDate.Date).Days / 7 + 1);

            var sessionsToCreate = new List<Session>();
            var baseSession = new Session
            {
                Id = Guid.NewGuid().ToString(),
                SectionId = section.Id,
                Type = request.Type,
                Date = sessionDate,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                RoomName = request.Room,
                Notes = request.Notes,
                SessionNumber = nextSessionNumber,
                WeekNumber = baseWeekNumber,
                AttendanceStatus = "pending",
                IsRecurring = request.IsRecurring
            };
            sessionsToCreate.Add(baseSession);

            if (request.IsRecurring)
            {
                var tempDate = sessionDate.AddDays(7);
                var tempNumber = nextSessionNumber + 1;
                while (tempDate.Date <= semester.EndDate.Date)
                {
                    var tempWeek = Math.Max(1, (tempDate - semester.StartDate.Date).Days / 7 + 1);
                    sessionsToCreate.Add(new Session
                    {
                        Id = Guid.NewGuid().ToString(),
                        SectionId = section.Id,
                        Type = request.Type,
                        Date = DateTime.SpecifyKind(tempDate.Date, DateTimeKind.Utc),
                        StartTime = request.StartTime,
                        EndTime = request.EndTime,
                        RoomName = request.Room,
                        Notes = request.Notes,
                        SessionNumber = tempNumber,
                        WeekNumber = tempWeek,
                        AttendanceStatus = "pending",
                        IsRecurring = true
                    });
                    tempDate = tempDate.AddDays(7);
                    tempNumber++;
                }
            }

            _context.Sessions.AddRange(sessionsToCreate);
            await _context.SaveChangesAsync();

            if (request.SendReminder)
            {
                var enrolledStudentIds = await _context.Enrollments
                    .AsNoTracking()
                    .Where(e => e.SectionId == section.Id && e.Status == Enums.EnrollmentStatus.Enrolled)
                    .Select(e => e.StudentId)
                    .ToListAsync();

                var courseName = section.Course?.Title ?? "your course";
                var typeStr = request.Type.ToString().ToLower();
                var dateStr = sessionDate.ToString("yyyy-MM-dd");
                var title = "New Session Scheduled";
                var body = $"A new {typeStr} has been scheduled for {courseName} on {dateStr} at {request.StartTime} in {request.Room}.";

                foreach (var studentId in enrolledStudentIds)
                {
                    await _notificationService.CreateNotificationAsync(
                        targetId: studentId,
                        type: "schedule",
                        title: title,
                        body: body,
                        actionUrl: $"/student/schedule",
                        isStudent: true
                    );
                }
            }

            return StatusCode(201, new
            {
                data = new
                {
                    session = new
                    {
                        id = baseSession.Id,
                        course_id = section.CourseId,
                        course_code = section.Course?.CourseCode ?? "",
                        course_name = section.Course?.Title ?? "",
                        session_number = baseSession.SessionNumber,
                        type = baseSession.Type.ToString().ToLower(),
                        date = baseSession.Date.ToString("yyyy-MM-dd"),
                        start_time = baseSession.StartTime,
                        end_time = baseSession.EndTime,
                        room = baseSession.RoomName,
                        week_number = baseSession.WeekNumber,
                        attendance_status = baseSession.AttendanceStatus,
                        is_recurring = baseSession.IsRecurring
                    }
                }
            });
        }

        [HttpPatch("sessions/{session_id}")]
        public async Task<IActionResult> UpdateSession(string session_id, [FromBody] UpdateSessionRequest request)
        {
            var userId = GetUserId();
            var instructor = await _context.Instructors.AsNoTracking().FirstOrDefaultAsync(i => i.UserId == userId);
            if (instructor == null) return Unauthorized();

            var session = await _context.Sessions
                .Include(s => s.Section)
                    .ThenInclude(sec => sec!.Course)
                .FirstOrDefaultAsync(s => s.Id == session_id);

            if (session == null) return NotFound();

            if (session.Section?.InstructorId != instructor.Id) return Forbid();

            var proposedType = request.Type ?? session.Type;
            var proposedDate = session.Date;
            if (!string.IsNullOrEmpty(request.Date))
            {
                if (!DateTime.TryParse(request.Date, out proposedDate))
                {
                    return BadRequest(new { error = "VALIDATION_ERROR", message = _localizer.GetMessage("INVALID_DATE_FORMAT") });
                }
                proposedDate = DateTime.SpecifyKind(proposedDate.Date, DateTimeKind.Utc);
            }
            else
            {
                proposedDate = DateTime.SpecifyKind(proposedDate.Date, DateTimeKind.Utc);
            }
            var proposedStartTime = request.StartTime ?? session.StartTime;
            var proposedEndTime = request.EndTime ?? session.EndTime;
            var proposedRoom = request.Room ?? session.RoomName;

            var conflicts = await GetConflictingSessionsAsync(
                courseId: session.Section.CourseId,
                instructorId: instructor.Id,
                date: proposedDate,
                startTime: proposedStartTime,
                endTime: proposedEndTime,
                room: proposedRoom,
                excludeSessionId: session.Id
            );

            if (conflicts.Any())
            {
                return Conflict(new
                {
                    error = new
                    {
                        code = "SCHEDULE_CONFLICT",
                        message = _localizer.GetMessage("SCHEDULE_CONFLICT"),
                        details = new
                        {
                            conflicting_sessions = conflicts.Select(s => new
                            {
                                session_id = s.Id,
                                course_code = s.Section?.Course?.CourseCode ?? "",
                                course_name = s.Section?.Course?.Title ?? "",
                                date = s.Date.ToString("yyyy-MM-dd"),
                                start_time = s.StartTime,
                                end_time = s.EndTime,
                                room = s.RoomName
                            }).ToList()
                        }
                    }
                });
            }

            if (request.Type.HasValue) session.Type = request.Type.Value;
            if (!string.IsNullOrEmpty(request.Date)) session.Date = proposedDate;
            if (!string.IsNullOrEmpty(request.StartTime)) session.StartTime = proposedStartTime;
            if (!string.IsNullOrEmpty(request.EndTime)) session.EndTime = proposedEndTime;
            if (!string.IsNullOrEmpty(request.Room)) session.RoomName = proposedRoom;

            if (ModelState.ContainsKey("notes") || ModelState.ContainsKey("Notes"))
            {
                session.Notes = request.Notes;
            }

            if (!string.IsNullOrEmpty(request.Date))
            {
                var semester = await _context.Semesters.AsNoTracking().FirstOrDefaultAsync(s => s.Id == session.Section.SemesterId);
                if (semester != null)
                {
                    session.WeekNumber = Math.Max(1, (proposedDate - semester.StartDate.Date).Days / 7 + 1);
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                data = new
                {
                    session = new
                    {
                        id = session.Id,
                        course_id = session.Section.CourseId,
                        course_code = session.Section.Course?.CourseCode ?? "",
                        course_name = session.Section.Course?.Title ?? "",
                        session_number = session.SessionNumber,
                        type = session.Type.ToString().ToLower(),
                        date = session.Date.ToString("yyyy-MM-dd"),
                        start_time = session.StartTime,
                        end_time = session.EndTime,
                        room = session.RoomName,
                        week_number = session.WeekNumber,
                        attendance_status = session.AttendanceStatus,
                        is_recurring = session.IsRecurring
                    }
                }
            });
        }

        [HttpDelete("sessions/{session_id}")]
        public async Task<IActionResult> DeleteSession(string session_id, [FromQuery] bool delete_recurring = false)
        {
            var userId = GetUserId();
            var instructor = await _context.Instructors.AsNoTracking().FirstOrDefaultAsync(i => i.UserId == userId);
            if (instructor == null) return Unauthorized();

            var session = await _context.Sessions
                .AsNoTracking()
                .Include(s => s.Section)
                .FirstOrDefaultAsync(s => s.Id == session_id);

            if (session == null) return NotFound();

            if (session.Section?.InstructorId != instructor.Id) return Forbid();

            var hasAttendance = session.AttendanceStatus == "completed" || 
                                session.AttendanceStatus == "in_progress" ||
                                await _context.AttendanceRecords.AsNoTracking().AnyAsync(a => a.SessionId == session_id && a.Status != "pending");

            if (hasAttendance)
            {
                return Conflict(new
                {
                    error = new
                    {
                        code = "ATTENDANCE_EXISTS",
                        message = _localizer.GetMessage("ATTENDANCE_EXISTS")
                    }
                });
            }

            if (delete_recurring && session.IsRecurring)
            {
                var futureSessionsQuery = _context.Sessions
                    .Where(s => s.SectionId == session.SectionId &&
                                s.Date >= session.Date &&
                                s.StartTime == session.StartTime &&
                                s.EndTime == session.EndTime &&
                                s.RoomName == session.RoomName &&
                                s.IsRecurring);

                bool anyFutureHasAttendance = await _context.AttendanceRecords.AsNoTracking()
                    .AnyAsync(a => futureSessionsQuery.Select(fs => fs.Id).Contains(a.SessionId) && a.Status != "pending");

                if (anyFutureHasAttendance)
                {
                    return Conflict(new
                    {
                        error = new
                        {
                            code = "ATTENDANCE_EXISTS",
                            message = _localizer.GetMessage("ATTENDANCE_EXISTS_FUTURE")
                        }
                    });
                }

                await futureSessionsQuery.ExecuteDeleteAsync();
            }
            else
            {
                await _context.Sessions.Where(s => s.Id == session_id).ExecuteDeleteAsync();
            }

            return NoContent();
        }

        private async Task<List<Session>> GetConflictingSessionsAsync(
            string courseId,
            string instructorId,
            DateTime date,
            string startTime,
            string endTime,
            string? room,
            string? excludeSessionId = null)
        {
            var utcDate = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
            var query = _context.Sessions
                .AsNoTracking()
                .Include(s => s.Section)
                    .ThenInclude(sec => sec!.Course)
                .Where(s => s.Date == utcDate &&
                            s.StartTime.CompareTo(endTime) < 0 && 
                            s.EndTime.CompareTo(startTime) > 0);

            if (!string.IsNullOrEmpty(excludeSessionId))
            {
                query = query.Where(s => s.Id != excludeSessionId);
            }

            var sessions = await query.ToListAsync();

            return sessions.Where(s => 
                (!string.IsNullOrEmpty(room) && s.RoomName.Equals(room, StringComparison.OrdinalIgnoreCase)) ||
                s.Section!.InstructorId == instructorId ||
                s.Section.CourseId == courseId
            ).ToList();
        }
    }
}
