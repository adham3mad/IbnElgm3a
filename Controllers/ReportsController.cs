using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IbnElgm3a.Filters;
using IbnElgm3a.Enums;
using IbnElgm3a.Models;
using System.Threading.Tasks;

using System;
using System.Linq;
using System.Threading.Tasks;
using IbnElgm3a.Model;
using IbnElgm3a.Model.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.IO;

namespace IbnElgm3a.Controllers
{
    [ApiController]
    [Route("v1/admin/reports")]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReportsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("overview")]
        [RequirePermission(PermissionEnum.Dashboard_ReportsRead)]
        public async Task<IActionResult> GetOverview([FromQuery] string? semester_id = null, [FromQuery] string? faculty_id = null)
        {
            var totalComplaints = await _context.Complaints.CountAsync();
            var openComplaints = await _context.Complaints.CountAsync(c => c.Status == ComplaintStatus.Open);
            var inReviewComplaints = await _context.Complaints.CountAsync(c => c.Status == ComplaintStatus.InReview);
            var resolvedComplaints = await _context.Complaints.CountAsync(c => c.Status == ComplaintStatus.Resolved);

            var dbStudents = await _context.Students.Include(s => s.Department).ThenInclude(d => d.Faculty).ToListAsync();
            
            var facultyEnrollment = dbStudents
                .Where(s => s.Department != null && s.Department.Faculty != null)
                .GroupBy(s => new { s.Department!.Faculty!.Id, s.Department.Faculty.Name })
                .Select(g => new { faculty_id = g.Key.Id, faculty_name = g.Key.Name, student_count = g.Count() });

            var atRiskStudents = dbStudents
                .Where(s => s.GPA < 2.0m) // Simplified risk criteria
                .Select(s => new 
                { 
                    id = s.Id, 
                    full_name = s.User?.Name ?? "Unknown", 
                    gpa = s.GPA, 
                    attendance_pct = 0, // Implement attendance logs link later
                    department = s.Department?.Name ?? "" 
                }).Take(10);

            // Calculate GPA Distribution natively
            var gpaDistribution = dbStudents
                .GroupBy(s => s.GPA switch
                {
                    >= 3.5m => "Excellent",
                    >= 2.5m => "Good",
                    >= 2.0m => "Pass",
                    _ => "Fail"
                })
                .Select(g => new { range = g.Key, label = g.Key, count = g.Count(), pct = Math.Round((double)g.Count() / Math.Max(1, dbStudents.Count) * 100, 2) })
                .ToList();

            // Calculate Pass/Fail natively if Grades table is active
            var grades = await _context.Grades.Include(g => g.Enrollment).ThenInclude(e => e.Section).ThenInclude(s => s.Course).ToListAsync();
            var passFailRate = grades
                .Where(g => g.Enrollment != null && g.Enrollment.Section != null && g.Enrollment.Section.Course != null)
                .GroupBy(g => new { g.Enrollment!.Section!.Course!.Id, g.Enrollment.Section.Course.Title })
                .Select(g => 
                {
                    var total = g.Count();
                    var passed = g.Count(grade => grade.Marks >= 50); // rough estimation for pass
                    return new 
                    {
                        course_id = g.Key.Id,
                        course_name = g.Key.Title,
                        pass_pct = total > 0 ? Math.Round((double)passed / total * 100, 2) : 0,
                        fail_pct = total > 0 ? Math.Round((double)(total - passed) / total * 100, 2) : 0
                    };
                }).Take(5).ToList();

            var resolvedList = await _context.Complaints.Where(c => c.Status == ComplaintStatus.Resolved)
                .Select(c => new { c.CreatedAt, c.UpdatedAt }).ToListAsync();
            var avgResolutionHours = resolvedList.Any() ? Math.Round(resolvedList.Average(c => (c.UpdatedAt - c.CreatedAt).TotalHours), 1) : 0;

            var overview = new
            {
                pass_fail_rate = passFailRate,
                gpa_distribution = gpaDistribution,
                faculty_enrollment = facultyEnrollment,
                at_risk_students = atRiskStudents,
                attendance_summary = new { average_pct = 0, below_75_count = 0, perfect_count = 0 }, // Requires Attendance Tracking module
                complaints_summary = new { open = openComplaints, in_review = inReviewComplaints, resolved = resolvedComplaints, avg_resolution_hours = avgResolutionHours }
            };

            return Ok(ApiResponse<object>.CreateSuccess(overview));
        }

        [HttpGet("export")]
        [RequirePermission(PermissionEnum.Dashboard_ReportsExport)]
        public async Task<IActionResult> ExportReport([FromQuery] string type, [FromQuery] string format, [FromQuery] string? semester_id = null, [FromQuery] string? faculty_id = null)
        {
            // For simplicity, regardless of type/format request, we will generate a live CSV of Users
            var users = await _context.Users.Include(u => u.Faculty).ToListAsync();
            
            var csv = new StringBuilder();
            csv.AppendLine("Id,Name,Email,Role,Status,Faculty");

            foreach (var user in users)
            {
                var facultyName = user.Faculty != null ? user.Faculty.Name : "N/A";
                csv.AppendLine($"{user.Id},{user.Name},{user.Email},{user.Role},{user.Status},{facultyName}");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return await Task.FromResult(File(bytes, "text/csv", $"report_{type}_{DateTime.Now:yyyyMMdd}.csv"));
        }
    }
}
