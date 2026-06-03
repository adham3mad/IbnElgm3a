using IbnElgm3a.Attributes;
using IbnElgm3a.Models;
using IbnElgm3a.Models.Data;
using IbnElgm3a.Enums;
using IbnElgm3a.Services;
using IbnElgm3a.Services.Localization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IbnElgm3a.Controllers.Instructors
{
    [ApiController]
    [Route("instructor/courses")]
    [Authorize(Roles = "instructor")]
    public class InstructorCoursesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILocalizationService _localizer;

        public InstructorCoursesController(AppDbContext context, ILocalizationService localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        [HttpGet]
        public async Task<IActionResult> GetCourses([FromQuery] string? semester, [FromQuery] InstructorCourseStatus status = InstructorCourseStatus.Active)
        {
            var userId = GetUserId();

            var instructorId = await _context.Instructors
                .AsNoTracking()
                .Where(i => i.UserId == userId)
                .Select(i => i.Id)
                .FirstOrDefaultAsync();

            if (instructorId == null) return Unauthorized();

            var now = DateTimeOffset.UtcNow;

            // Fetch active semester (lightweight, no Entity tracking)
            var activeSemester = await _context.Semesters
                .AsNoTracking()
                .Where(s => s.StartDate <= now && s.EndDate >= now)
                .Select(s => new { s.Id, s.Name, s.StartDate, s.EndDate, s.TotalWeeks })
                .FirstOrDefaultAsync()
                ?? await _context.Semesters
                    .AsNoTracking()
                    .OrderByDescending(s => s.StartDate)
                    .Select(s => new { s.Id, s.Name, s.StartDate, s.EndDate, s.TotalWeeks })
                    .FirstOrDefaultAsync();

            var currentWeek = activeSemester != null
                ? Math.Max(1, (now - activeSemester.StartDate).Days / 7 + 1)
                : 1;

            // Build section query with proper status filter using real date comparison
            var sectionQuery = _context.Sections
                .AsNoTracking()
                .Where(s => s.InstructorId == instructorId);

            if (!string.IsNullOrEmpty(semester))
            {
                sectionQuery = sectionQuery.Where(s => s.Course!.Semester!.Name == semester);
            }

            if (status == InstructorCourseStatus.Active)
            {
                // Fix: use real date comparison instead of IsActive flag
                sectionQuery = sectionQuery.Where(s =>
                    s.Course!.Semester!.StartDate <= now && s.Course.Semester.EndDate >= now);
            }

            // Fetch all data in a single projection query - no N+1
            var sectionsData = await sectionQuery
                .Select(s => new
                {
                    CourseId = s.CourseId,
                    CourseCode = s.Course!.CourseCode,
                    CourseName = s.Course.Title,
                    SemesterName = s.Course.Semester != null ? s.Course.Semester.Name : "",
                    SemesterTotalWeeks = s.Course.Semester != null ? s.Course.Semester.TotalWeeks : 14,
                    StudentCount = s.Enrollments.Count(e => e.Status == EnrollmentStatus.Enrolled),
                    PendingSubmissionsCount = _context.AssignmentSubmissions
                        .Count(sub => sub.Assignment!.CourseId == s.CourseId && sub.Status == "submitted"),
                    ScheduleSummary = s.ScheduleSlots
                        .Select(ss => ss.Day.ToString().Substring(0, 3) + " " + ss.StartTime)
                        .FirstOrDefault() ?? ""
                })
                .ToListAsync();

            // De-duplicate by course (a course can have multiple sections taught by same instructor)
            var courses = sectionsData
                .GroupBy(s => s.CourseId)
                .Select(g =>
                {
                    var first = g.First();
                    var totalWeeks = first.SemesterTotalWeeks == 0 ? 14 : first.SemesterTotalWeeks;
                    return new
                    {
                        id = first.CourseId,
                        code = first.CourseCode,
                        name = first.CourseName,
                        semester = first.SemesterName,
                        week_current = currentWeek,
                        week_total = totalWeeks,
                        student_count = g.Sum(s => s.StudentCount),
                        status = "active",
                        schedule_summary = first.ScheduleSummary,
                        progress_percent = totalWeeks > 0 ? (int)((double)currentWeek / totalWeeks * 100) : 0,
                        pending_submissions_count = g.First().PendingSubmissionsCount
                    };
                })
                .OrderBy(c => c.code)
                .ToList();

            return Ok(new
            {
                semester = activeSemester?.Name ?? "",
                courses = courses
            });
        }

        [HttpGet("{course_id}")]
        public async Task<IActionResult> GetCourseDetail(string course_id)
        {
            var userId = GetUserId();

            var instructorId = await _context.Instructors
                .AsNoTracking()
                .Where(i => i.UserId == userId)
                .Select(i => i.Id)
                .FirstOrDefaultAsync();

            if (instructorId == null) return Unauthorized();

            var course = await _context.Courses
                .AsNoTracking()
                .Include(c => c.Semester)
                .FirstOrDefaultAsync(c => c.Id == course_id);

            if (course == null) return NotFound();

            // Verify instructor teaches this course
            var isTeaching = await _context.Sections
                .AsNoTracking()
                .AnyAsync(s => s.CourseId == course_id && s.InstructorId == instructorId);

            if (!isTeaching) return Forbid();

            var now = DateTimeOffset.UtcNow;
            var currentWeek = course.Semester != null
                ? (now - course.Semester.StartDate).Days / 7 + 1
                : 1;

            // Sequential queries to avoid EF Core DbContext concurrency exceptions
            var studentCount = await _context.Enrollments
                .AsNoTracking()
                .CountAsync(e => e.Section!.CourseId == course_id && e.Status == EnrollmentStatus.Enrolled);

            var schedule = await _context.ScheduleSlots
                .AsNoTracking()
                .Where(ss => ss.Section!.CourseId == course_id && ss.Section.InstructorId == instructorId)
                .Select(ss => new
                {
                    day_of_week = ss.Day.ToString().ToLower(),
                    start_time = ss.StartTime,
                    end_time = ss.EndTime,
                    room = ss.Room != null ? ss.Room.Name : "",
                    type = ss.Type.ToString().ToLower()
                })
                .ToListAsync();

            var gradesList = (await _context.Grades
                .AsNoTracking()
                .Where(g => g.Enrollment!.Section!.CourseId == course_id)
                .ToListAsync())
                .Select(g => (double)g.Marks)
                .ToList();

            var attendanceRecords = await _context.AttendanceRecords
                .AsNoTracking()
                .Where(a => a.Session!.Section!.CourseId == course_id && a.Session.AttendanceStatus == "completed")
                .Select(a => new { a.SessionId, a.StudentId, a.Status })
                .ToListAsync();

            var pendingSubmissions = await _context.AssignmentSubmissions
                .AsNoTracking()
                .CountAsync(s => s.Assignment!.CourseId == course_id && s.Status == "submitted");

            var completedSessionsCount = await _context.Sessions
                .AsNoTracking()
                .CountAsync(s => s.Section!.CourseId == course_id && s.AttendanceStatus == "completed");

            var enrolledStudentIds = await _context.Enrollments
                .AsNoTracking()
                .Where(e => e.Section!.CourseId == course_id && e.Status == EnrollmentStatus.Enrolled)
                .Select(e => e.StudentId)
                .ToListAsync();

            var classAverage = gradesList.Any() ? gradesList.Average() : 0.0;

            var sessionsGrouped = attendanceRecords.GroupBy(a => a.SessionId).ToList();
            var averageAttendanceRate = sessionsGrouped.Any()
                ? sessionsGrouped.Average(g => (double)g.Count(a => a.Status == "present" || a.Status == "late") / g.Count())
                : 0.0;

            var studentGroups = attendanceRecords.GroupBy(a => a.StudentId).ToDictionary(g => g.Key, g => g.ToList());

            int atRiskCount = 0;
            if (completedSessionsCount > 0)
            {
                foreach (var studentId in enrolledStudentIds)
                {
                    if (studentGroups.TryGetValue(studentId, out var records) && records.Any())
                    {
                        double rate = (double)records.Count(r => r.Status == "present" || r.Status == "late") / records.Count;
                        if (rate < 0.6) atRiskCount++;
                    }
                    else
                    {
                        atRiskCount++;
                    }
                }
            }

            var totalWeeks = course.Semester != null
                ? (course.Semester.TotalWeeks == 0 ? 14 : course.Semester.TotalWeeks)
                : 14;

            return Ok(new
            {
                id = course.Id,
                code = course.CourseCode,
                name = course.Title,
                semester = course.Semester?.Name ?? "",
                week_current = currentWeek,
                week_total = totalWeeks,
                student_count = studentCount,
                status = "active",
                schedule_summary = schedule.Any() ? $"{schedule.First().day_of_week} {schedule.First().start_time}" : "",
                progress_percent = totalWeeks > 0 ? (int)((double)currentWeek / totalWeeks * 100) : 0,
                pending_submissions_count = pendingSubmissions,
                overview = new
                {
                    class_average = classAverage,
                    average_attendance_rate = averageAttendanceRate,
                    to_grade_count = pendingSubmissions,
                    at_risk_count = atRiskCount
                },
                recurring_schedule = schedule
            });
        }

        [HttpGet("{course_id}/materials")]
        public async Task<IActionResult> GetMaterials(string course_id, [FromQuery] int? week_number)
        {
            var query = _context.CourseMaterials
                .AsNoTracking()
                .Where(m => m.CourseId == course_id);

            if (week_number.HasValue)
            {
                query = query.Where(m => m.WeekNumber == week_number.Value);
            }

            var materials = await query
                .OrderByDescending(m => m.WeekNumber)
                .ThenBy(m => m.CreatedAt)
                .Select(m => new
                {
                    WeekNumber = m.WeekNumber,
                    id = m.Id,
                    title = m.Title,
                    type = m.Type,
                    file_url = m.FileUrl,
                    external_url = m.ExternalUrl,
                    file_size_bytes = m.FileSizeBytes,
                    duration_seconds = m.DurationSeconds,
                    status = m.Status,
                    view_count = m.ViewCount,
                    created_at = m.CreatedAt
                })
                .ToListAsync();

            var grouped = materials.GroupBy(m => m.WeekNumber)
                .Select(g => new
                {
                    week_number = g.Key,
                    week_label = $"Week {g.Key}",
                    materials = g.Select(m => new
                    {
                        m.id,
                        m.title,
                        m.type,
                        m.file_url,
                        m.external_url,
                        m.file_size_bytes,
                        m.duration_seconds,
                        m.status,
                        m.view_count,
                        m.created_at
                    }).ToList()
                }).ToList();

            return Ok(new { weeks = grouped });
        }

        [HttpGet("{course_id}/roster")]
        public async Task<IActionResult> GetRoster(string course_id, [FromQuery] RosterRiskStatus? risk_status, [FromQuery] int page = 1, [FromQuery] int limit = 50)
        {
            // Sequential queries to avoid EF Core DbContext concurrency exceptions
            var enrollments = await _context.Enrollments
                .AsNoTracking()
                .Where(e => e.Section!.CourseId == course_id && e.Status == EnrollmentStatus.Enrolled)
                .Select(e => new
                {
                    StudentId = e.Student != null ? e.Student.Id : "",
                    AcademicNumber = e.Student != null ? e.Student.AcademicNumber : "",
                    UserName = (e.Student != null && e.Student.User != null) ? e.Student.User.Name : ""
                })
                .ToListAsync();

            var completedSessionsCount = await _context.Sessions
                .AsNoTracking()
                .CountAsync(s => s.Section!.CourseId == course_id && s.AttendanceStatus == "completed");

            var attendanceRecords = await _context.AttendanceRecords
                .AsNoTracking()
                .Where(a => a.Session!.Section!.CourseId == course_id && a.Session.AttendanceStatus == "completed")
                .Select(a => new { a.StudentId, a.Status })
                .ToListAsync();

            var students = enrollments.Select(e =>
            {
                var studentRecords = attendanceRecords.Where(a => a.StudentId == e.StudentId).ToList();
                var presentCount = studentRecords.Count(a => a.Status == "present" || a.Status == "late");

                var attendanceRate = completedSessionsCount > 0 ? (float)presentCount / completedSessionsCount : 1.0f;
                var riskStatus = attendanceRate >= 0.75f ? "good" : (attendanceRate >= 0.60f ? "watch" : "at_risk");

                var userName = e.UserName ?? "";
                var nameParts = userName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var firstName = nameParts.Length > 0 ? nameParts[0] : "";
                var lastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "";
                var initials = "";
                if (firstName.Length > 0)
                {
                    initials += firstName.Substring(0, 1).ToUpper();
                }
                if (lastName.Length > 0)
                {
                    var lastPart = nameParts.Length > 1 ? nameParts[1] : "";
                    if (lastPart.Length > 0)
                    {
                        initials += lastPart.Substring(0, 1).ToUpper();
                    }
                }

                return new
                {
                    id = e.StudentId,
                    student_number = e.AcademicNumber,
                    first_name = firstName,
                    last_name = lastName,
                    full_name = userName,
                    initials = initials,
                    attendance_rate = attendanceRate,
                    risk_status = riskStatus
                };
            }).ToList();

            if (risk_status.HasValue)
            {
                var riskStr = risk_status.Value switch
                {
                    RosterRiskStatus.AtRisk => "at_risk",
                    _ => risk_status.Value.ToString().ToLower()
                };
                students = students.Where(s => s.risk_status == riskStr).ToList();
            }

            var totalItems = students.Count;
            var pagedStudents = students.Skip((page - 1) * limit).Take(limit).ToList();

            return Ok(new
            {
                data = new { students = pagedStudents },
                meta = new
                {
                    page = page,
                    limit = limit,
                    total_items = totalItems,
                    total_pages = (int)Math.Ceiling((double)totalItems / limit)
                }
            });
        }
    }
}
