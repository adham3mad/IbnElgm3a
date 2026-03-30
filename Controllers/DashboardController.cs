using IbnElgm3a.DTOs.Dashboard;
using IbnElgm3a.Models;
using IbnElgm3a.Filters;
using IbnElgm3a.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using IbnElgm3a.Model;

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
            var openComplaints = await _context.Complaints.CountAsync(c => c.Status == IbnElgm3a.Enums.ComplaintStatus.Open);

            var activeSemester = await _context.Semesters.OrderByDescending(s => s.StartDate).FirstOrDefaultAsync();
            var dbSemesterId = semester_id ?? activeSemester?.Id ?? "sem_1";
            var dbSemesterName = activeSemester?.Name ?? "Active Semester";
            var weeks = 15; // standard
            var currentWeek = activeSemester != null ? (System.DateTimeOffset.UtcNow - activeSemester.StartDate).Days / 7 : 1;

            var dashboardData = new DashboardResponseDto
            {
                Stats = new DashboardStatsDto
                {
                    TotalStudents = totalStudents,
                    TotalInstructors = totalInstructors,
                    TotalCourses = totalCourses,
                    OpenComplaints = openComplaints
                },
                Semester = new SemesterInfoDto
                {
                    Id = dbSemesterId,
                    Name = dbSemesterName,
                    CurrentWeek = currentWeek,
                    TotalWeeks = weeks,
                    NextEvent = "Pending Calendar Load"
                },
                Alerts = new System.Collections.Generic.List<AlertDto>
                {
                    new AlertDto { Type = "overdue_complaints", Message = "Open Complaints", Count = openComplaints }
                },
                ModuleBadges = new System.Collections.Generic.Dictionary<string, int>
                {
                    { "complaints", openComplaints }
                }
            };

            return Ok(ApiResponse<DashboardResponseDto>.CreateSuccess(dashboardData));
        }
    }
}
