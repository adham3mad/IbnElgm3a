using IbnElgm3a.DTOs.Dashboard;
using IbnElgm3a.Models;
using IbnElgm3a.Filters;
using IbnElgm3a.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using IbnElgm3a.Models;

namespace IbnElgm3a.Controllers
{
    [ApiController]
    [Route("v1/admin/dashboard")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [RequirePermission(PermissionEnum.Dashboard_Main_Read)]
        public async Task<IActionResult> GetDashboard([FromQuery] string? semester_id = null)
        {
            var totalStudents = await _context.Students.CountAsync();
            var totalInstructors = await _context.Instructors.CountAsync();
            var totalCourses = await _context.Courses.CountAsync();
            var openComplaints = await _context.Complaints.CountAsync(c => c.Status != IbnElgm3a.Enums.ComplaintStatus.Resolved);

            var activeSemester = await _context.Semesters.OrderByDescending(s => s.StartDate).FirstOrDefaultAsync();
            var semester = activeSemester;
            if (semester_id != null) semester = await _context.Semesters.FindAsync(semester_id);

            // Calculate university-wide pass rate
            var totalGrades = await _context.Grades.CountAsync();
            var passingGrades = await _context.Grades.CountAsync(g => g.LetterGrade != LetterGrade.F);
            var universityPassRate = totalGrades > 0 ? (decimal)passingGrades / totalGrades * 100 : 0;

            // Fetch faculties with summaries
            var faculties = await _context.Faculties
                .Select(f => new FacultySummaryDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    Code = f.Code,
                    StudentCount = _context.Users.Count(u => u.FacultyId == f.Id && u.Role != null && u.Role.Name == "student"),
                    DeptCount = _context.Departments.Count(d => d.FacultyId == f.Id),
                    AlertCount = _context.Complaints.Count(c => c.Student != null && c.Student.FacultyId == f.Id && c.Status != ComplaintStatus.Resolved),
                    PassRate = _context.Grades.Count(g => g.Enrollment != null && g.Enrollment.Section != null && g.Enrollment.Section.Course != null && g.Enrollment.Section.Course.Department != null && g.Enrollment.Section.Course.Department.FacultyId == f.Id) > 0 
                        ? (decimal)_context.Grades.Count(g => g.Enrollment != null && g.Enrollment.Section != null && g.Enrollment.Section.Course != null && g.Enrollment.Section.Course.Department != null && g.Enrollment.Section.Course.Department.FacultyId == f.Id && g.LetterGrade != LetterGrade.F) 
                          / _context.Grades.Count(g => g.Enrollment != null && g.Enrollment.Section != null && g.Enrollment.Section.Course != null && g.Enrollment.Section.Course.Department != null && g.Enrollment.Section.Course.Department.FacultyId == f.Id) * 100 
                        : 0
                }).ToListAsync();

            var recentActivity = await _context.AuditLogs
                .OrderByDescending(a => a.CreatedAt)
                .Take(10)
                .Select(a => new ActivityDto
                {
                    Icon = a.Action == "CREATE" ? "plus" : a.Action == "DELETE" ? "trash" : "edit",
                    Text = $"{a.Action} {a.EntityName}",
                    PerformedAt = a.CreatedAt,
                    ActorName = a.User != null ? a.User.Email : "System"
                }).ToListAsync();

            var dashboardData = new DashboardResponseDto
            {
                Stats = new DashboardStatsDto
                {
                    TotalStudents = totalStudents,
                    TotalInstructors = totalInstructors,
                    TotalCourses = totalCourses,
                    OpenComplaints = openComplaints,
                    PassRate = Math.Round(universityPassRate, 2)
                },
                Semester = new SemesterInfoDto
                {
                    Id = semester?.Id ?? "sem_1",
                    Name = semester?.Name ?? "Spring 2025",
                    CurrentWeek = semester != null ? (DateTimeOffset.UtcNow - semester.StartDate).Days / 7 + 1 : 1,
                    TotalWeeks = semester?.TotalWeeks ?? 16,
                    NextEvent = semester?.NextEvent ?? "Finals in 5 weeks"
                },
                Faculties = faculties,
                Alerts = new List<AlertDto>
                {
                    new AlertDto { Type = "urgent", Message = "Urgent Complaints", Count = openComplaints }
                },
                RecentActivity = recentActivity,
                ModuleBadges = new Dictionary<string, int>
                {
                    { "complaints", openComplaints }
                }
            };

            return Ok(dashboardData);
        }

        [HttpGet("faculty")]
        public async Task<IActionResult> GetFacultyDashboard([FromQuery] string faculty_id)
        {
            var faculty = await _context.Faculties.FindAsync(faculty_id);
            if (faculty == null) return NotFound(ApiResponse<object>.CreateError("NOT_FOUND", "Faculty not found"));

            var studentCount = await _context.Users.CountAsync(u => u.FacultyId == faculty_id && u.Role != null && u.Role.Name == "student");
            var deptCount = await _context.Departments.CountAsync(d => d.FacultyId == faculty_id);
            var courseCount = await _context.Courses.CountAsync(c => c.Department != null && c.Department.FacultyId == faculty_id);
            var openComplaints = await _context.Complaints.CountAsync(c => c.Student != null && c.Student.FacultyId == faculty_id && c.Status != ComplaintStatus.Resolved);

            // Pass rate by level (assuming level is 1-4)
            var passRateByLevel = new Dictionary<int, decimal>();
            for (int i = 1; i <= 4; i++)
            {
                var total = await _context.Grades.CountAsync(g => g.Enrollment != null && g.Enrollment.Student != null && g.Enrollment.Student.Department != null && g.Enrollment.Student.Department.FacultyId == faculty_id && g.Enrollment.Student.Level == i);
                var passing = await _context.Grades.CountAsync(g => g.Enrollment != null && g.Enrollment.Student != null && g.Enrollment.Student.Department != null && g.Enrollment.Student.Department.FacultyId == faculty_id && g.Enrollment.Student.Level == i && g.LetterGrade != LetterGrade.F);
                passRateByLevel[i] = total > 0 ? Math.Round((decimal)passing / total * 100, 2) : 0;
            }

            var depts = await _context.Departments
                .Where(d => d.FacultyId == faculty_id)
                .Select(d => new
                {
                    id = d.Id,
                    name = d.Name,
                    code = d.Code,
                    student_count = _context.Users.Count(u => u.DepartmentId == d.Id && u.Role != null && u.Role.Name == "student"),
                    pass_rate = _context.Grades.Count(g => g.Enrollment != null && g.Enrollment.Section != null && g.Enrollment.Section.Course != null && g.Enrollment.Section.Course.DepartmentId == d.Id) > 0
                        ? (decimal)_context.Grades.Count(g => g.Enrollment != null && g.Enrollment.Section != null && g.Enrollment.Section.Course != null && g.Enrollment.Section.Course.DepartmentId == d.Id && g.LetterGrade != LetterGrade.F)
                          / _context.Grades.Count(g => g.Enrollment != null && g.Enrollment.Section != null && g.Enrollment.Section.Course != null && g.Enrollment.Section.Course.DepartmentId == d.Id) * 100
                        : 0
                }).ToListAsync();

            return Ok(new
            {
                stats = new
                {
                    total_students = studentCount,
                    total_departments = deptCount,
                    total_courses = courseCount,
                    open_complaints = openComplaints
                },
                pass_rate_by_level = passRateByLevel,
                departments = depts
            });
        }

        [HttpGet("department")]
        public async Task<IActionResult> GetDepartmentDashboard([FromQuery] string dept_id)
        {
            var dept = await _context.Departments.FindAsync(dept_id);
            if (dept == null) return NotFound(ApiResponse<object>.CreateError("NOT_FOUND", "Department not found"));

            var instructorCount = await _context.Users.CountAsync(u => u.DepartmentId == dept_id && u.Role != null && u.Role.Name == "instructor");
            var courseCount = await _context.Courses.CountAsync(c => c.DepartmentId == dept_id);
            
            // Pass rate by course
            var courseStats = await _context.Courses
                .Where(c => c.DepartmentId == dept_id)
                .Select(c => new
                {
                    id = c.Id,
                    name = c.Title,
                    code = c.CourseCode,
                    pass_rate = _context.Grades.Count(g => g.Enrollment != null && g.Enrollment.Section != null && g.Enrollment.Section.CourseId == c.Id) > 0
                        ? (decimal)_context.Grades.Count(g => g.Enrollment != null && g.Enrollment.Section != null && g.Enrollment.Section.CourseId == c.Id && g.LetterGrade != LetterGrade.F)
                          / _context.Grades.Count(g => g.Enrollment != null && g.Enrollment.Section != null && g.Enrollment.Section.CourseId == c.Id) * 100
                        : 0
                }).ToListAsync();

            return Ok(new
            {
                stats = new
                {
                    total_instructors = instructorCount,
                    total_courses = courseCount
                },
                courses = courseStats
            });
        }
    }
}
