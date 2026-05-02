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
    [Route("instructor/gradebook")]
    [Authorize(Roles = "instructor")]
    public class InstructorGradebookController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILocalizationService _localizer;

        public InstructorGradebookController(AppDbContext context, ILocalizationService localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        [HttpGet("courses/{course_id}/gradebook")]
        public async Task<IActionResult> GetGradebook(string course_id)
        {
            var students = await _context.Enrollments
                .Include(e => e.Student)
                    .ThenInclude(s => s!.User)
                .Where(e => e.Section!.CourseId == course_id && e.Status == Enums.EnrollmentStatus.Enrolled)
                .Select(e => e.Student)
                .ToListAsync();

            var assignments = await _context.Assignments
                .Where(a => a.CourseId == course_id)
                .OrderBy(a => a.CreatedAt)
                .ToListAsync();

            var submissions = await _context.AssignmentSubmissions
                .Where(s => assignments.Select(a => a.Id).Contains(s.AssignmentId))
                .ToListAsync();

            var gradebook = students.Select(s => new
            {
                student_id = s!.Id,
                full_name = s.User!.Name,
                student_number = s.AcademicNumber,
                grades = assignments.Select(a => new
                {
                    assignment_id = a.Id,
                    assignment_title = a.Title,
                    score = submissions.FirstOrDefault(sub => sub.StudentId == s.Id && sub.AssignmentId == a.Id)?.Score,
                    max_points = a.MaxPoints
                }).ToList(),
                total_score = submissions.Where(sub => sub.StudentId == s.Id).Sum(sub => sub.Score ?? 0),
                total_max = assignments.Sum(a => a.MaxPoints)
            }).ToList();

            return Ok(new
            {
                data = new
                {
                    assignments = assignments.Select(a => new { a.Id, a.Title, a.MaxPoints }),
                    gradebook = gradebook
                }
            });
        }

        [HttpGet("courses/{course_id}/students/{student_id}/report")]
        public async Task<IActionResult> GetStudentReport(string course_id, string student_id)
        {
            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == student_id);

            if (student == null) return NotFound();

            var assignments = await _context.Assignments
                .Where(a => a.CourseId == course_id)
                .OrderBy(a => a.CreatedAt)
                .ToListAsync();

            var submissions = await _context.AssignmentSubmissions
                .Where(s => s.StudentId == student_id && assignments.Select(a => a.Id).Contains(s.AssignmentId))
                .ToListAsync();

            var sessions = await _context.Sessions
                .Where(s => s.Section!.CourseId == course_id && s.AttendanceStatus == "completed")
                .ToListAsync();

            var attendance = await _context.AttendanceRecords
                .Where(a => a.StudentId == student_id && sessions.Select(sess => sess.Id).Contains(a.SessionId))
                .ToListAsync();

            return Ok(new
            {
                data = new
                {
                    student_info = new
                    {
                        full_name = student.User!.Name,
                        student_number = student.AcademicNumber,
                        email = student.User.Email
                    },
                    academic_summary = new
                    {
                        assignments_grade = assignments.Sum(a => a.MaxPoints) > 0 ? (float)submissions.Sum(s => s.Score ?? 0) / assignments.Sum(a => a.MaxPoints) : 0,
                        attendance_rate = sessions.Count > 0 ? (float)attendance.Count(a => a.Status == "present" || a.Status == "late") / sessions.Count : 0
                    },
                    assignment_details = assignments.Select(a => new
                    {
                        title = a.Title,
                        score = submissions.FirstOrDefault(s => s.AssignmentId == a.Id)?.Score,
                        max_points = a.MaxPoints,
                        status = submissions.FirstOrDefault(s => s.AssignmentId == a.Id)?.Status ?? "not_submitted"
                    }).ToList()
                }
            });
        }
    }
}
