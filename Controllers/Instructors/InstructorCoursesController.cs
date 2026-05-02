using IbnElgm3a.Models;
using IbnElgm3a.Models.Data;
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
        public async Task<IActionResult> GetCourses([FromQuery] string? semester, [FromQuery] string status = "active")
        {
            var userId = GetUserId();
            var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.UserId == userId);
            if (instructor == null) return Unauthorized();

            var now = DateTimeOffset.UtcNow;
            var query = _context.Sections
                .Include(s => s.Course)
                    .ThenInclude(c => c!.Semester)
                .Where(s => s.InstructorId == instructor.Id);

            if (!string.IsNullOrEmpty(semester))
            {
                query = query.Where(s => s.Course!.Semester!.Name == semester);
            }
            
            // Simplified status filter
            if (status == "active")
            {
                query = query.Where(s => s.Course!.Semester!.StartDate <= now && s.Course!.Semester!.EndDate >= now);
            }

            var courses = await query
                .Select(s => s.Course)
                .Distinct()
                .ToListAsync();

            var activeSemester = await _context.Semesters
                .Where(s => s.StartDate <= now && s.EndDate >= now)
                .FirstOrDefaultAsync() ?? await _context.Semesters.OrderByDescending(s => s.StartDate).FirstOrDefaultAsync();

            return Ok(new
            {
                data = new
                {
                    semester = activeSemester?.Name ?? "",
                    courses = courses.Select(c => new
                    {
                        id = c!.Id,
                        code = c.CourseCode,
                        name = c.Title,
                        semester = c.Semester?.Name ?? "",
                        week_current = activeSemester != null ? (now - activeSemester.StartDate).Days / 7 + 1 : 1,
                        week_total = c.Semester?.TotalWeeks ?? 14,
                        student_count = _context.Enrollments.Count(e => e.Section!.CourseId == c.Id && e.Status == Enums.EnrollmentStatus.Enrolled),
                        status = "active",
                        schedule_summary = _context.ScheduleSlots
                            .Where(ss => ss.Section!.CourseId == c.Id)
                            .Select(ss => ss.Day.ToString().Substring(0, 3) + " " + ss.StartTime)
                            .FirstOrDefault() ?? "",
                        progress_percent = activeSemester != null ? (int)((double)((now - activeSemester.StartDate).Days / 7 + 1) / activeSemester.TotalWeeks * 100) : 0,
                        pending_submissions_count = _context.AssignmentSubmissions.Count(s => s.Assignment!.CourseId == c.Id && s.Status == "submitted")
                    }).OrderBy(c => c.code).ToList()
                }
            });
        }

        [HttpGet("{course_id}")]
        public async Task<IActionResult> GetCourseDetail(string course_id)
        {
            var userId = GetUserId();
            var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.UserId == userId);
            if (instructor == null) return Unauthorized();

            var course = await _context.Courses
                .Include(c => c.Semester)
                .FirstOrDefaultAsync(c => c.Id == course_id);

            if (course == null) return NotFound();

            // Verify instructor teaches this course
            var isTeaching = await _context.Sections.AnyAsync(s => s.CourseId == course_id && s.InstructorId == instructor.Id);
            if (!isTeaching) return Forbid();

            var now = DateTimeOffset.UtcNow;
            var currentWeek = (now - course.Semester!.StartDate).Days / 7 + 1;
            var studentCount = await _context.Enrollments.CountAsync(e => e.Section!.CourseId == course_id && e.Status == Enums.EnrollmentStatus.Enrolled);

            var schedule = await _context.ScheduleSlots
                .Include(ss => ss.Room)
                .Where(ss => ss.Section!.CourseId == course_id && ss.Section.InstructorId == instructor.Id)
                .Select(ss => new
                {
                    day_of_week = ss.Day.ToString().ToLower(),
                    start_time = ss.StartTime,
                    end_time = ss.EndTime,
                    room = ss.Room!.Name,
                    type = ss.Type.ToString().ToLower()
                })
                .ToListAsync();

            return Ok(new
            {
                data = new
                {
                    id = course.Id,
                    code = course.CourseCode,
                    name = course.Title,
                    semester = course.Semester.Name,
                    week_current = currentWeek,
                    week_total = course.Semester.TotalWeeks,
                    student_count = studentCount,
                    status = "active",
                    schedule_summary = schedule.FirstOrDefault()?.day_of_week + " " + schedule.FirstOrDefault()?.start_time,
                    progress_percent = (int)((double)currentWeek / course.Semester.TotalWeeks * 100),
                    pending_submissions_count = await _context.AssignmentSubmissions.CountAsync(s => s.Assignment!.CourseId == course_id && s.Status == "submitted"),
                    overview = new
                    {
                        class_average = await _context.Grades
                            .Where(g => g.Enrollment!.Section!.CourseId == course_id)
                            .AverageAsync(g => (double?)g.Marks) ?? 0.0,
                        average_attendance_rate = await _context.AttendanceRecords
                            .Where(a => a.Session!.Section!.CourseId == course_id && a.Session.AttendanceStatus == "completed")
                            .GroupBy(a => a.SessionId)
                            .Select(g => (double)g.Count(a => a.Status == "present" || a.Status == "late") / g.Count())
                            .DefaultIfEmpty(0.0)
                            .AverageAsync(),
                        to_grade_count = await _context.AssignmentSubmissions.CountAsync(s => s.Assignment!.CourseId == course_id && s.Status == "submitted"),
                        at_risk_count = await _context.Enrollments
                            .Where(e => e.Section!.CourseId == course_id && e.Status == Enums.EnrollmentStatus.Enrolled)
                            .CountAsync(e => _context.AttendanceRecords
                                .Where(a => a.StudentId == e.StudentId && a.Session!.Section!.CourseId == course_id && a.Session.AttendanceStatus == "completed")
                                .GroupBy(a => a.StudentId)
                                .Select(g => (double)g.Count(a => a.Status == "present" || a.Status == "late") / g.Count())
                                .FirstOrDefault() < 0.6)
                    },
                    recurring_schedule = schedule
                }
            });
        }

        [HttpGet("{course_id}/materials")]
        public async Task<IActionResult> GetMaterials(string course_id, [FromQuery] int? week_number)
        {
            var query = _context.CourseMaterials.Where(m => m.CourseId == course_id);
            if (week_number.HasValue)
            {
                query = query.Where(m => m.WeekNumber == week_number.Value);
            }

            var materials = await query.OrderByDescending(m => m.WeekNumber).ThenBy(m => m.CreatedAt).ToListAsync();

            var grouped = materials.GroupBy(m => m.WeekNumber)
                .Select(g => new
                {
                    week_number = g.Key,
                    week_label = $"Week {g.Key}",
                    materials = g.Select(m => new
                    {
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
                    }).ToList()
                }).ToList();

            return Ok(new { data = new { weeks = grouped } });
        }

        [HttpGet("{course_id}/roster")]
        public async Task<IActionResult> GetRoster(string course_id, [FromQuery] string? risk_status, [FromQuery] int page = 1, [FromQuery] int limit = 50)
        {
            var enrollments = await _context.Enrollments
                .Include(e => e.Student)
                    .ThenInclude(s => s!.User)
                .Where(e => e.Section!.CourseId == course_id && e.Status == Enums.EnrollmentStatus.Enrolled)
                .ToListAsync();

            var completedSessionsCount = await _context.Sessions
                .CountAsync(s => s.Section!.CourseId == course_id && s.AttendanceStatus == "completed");

            var attendanceRecords = await _context.AttendanceRecords
                .Where(a => a.Session!.Section!.CourseId == course_id && a.Session.AttendanceStatus == "completed")
                .ToListAsync();

            var students = enrollments.Select(e =>
            {
                var student = e.Student!;
                var studentRecords = attendanceRecords.Where(a => a.StudentId == student.Id).ToList();
                var presentCount = studentRecords.Count(a => a.Status == "present" || a.Status == "late");
                
                var attendanceRate = completedSessionsCount > 0 ? (float)presentCount / completedSessionsCount : 1.0f; 
                var status = attendanceRate >= 0.75f ? "good" : (attendanceRate >= 0.60f ? "watch" : "at_risk");

                return new
                {
                    id = student.Id,
                    student_number = student.AcademicNumber,
                    first_name = student.User!.Name.Split(' ')[0],
                    last_name = student.User.Name.Contains(' ') ? student.User.Name.Split(' ')[1] : "",
                    full_name = student.User.Name,
                    initials = student.User.Name.Substring(0, 1) + (student.User.Name.Contains(' ') ? student.User.Name.Split(' ')[1].Substring(0, 1) : ""),
                    attendance_rate = attendanceRate,
                    risk_status = status
                };
            }).ToList();

            if (!string.IsNullOrEmpty(risk_status))
            {
                students = students.Where(s => s.risk_status == risk_status).ToList();
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
