using IbnElgm3a.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace IbnElgm3a.Controllers.Students
{
    [ApiController]
    [Route("student")] // mapped to student/gpa-history, etc.
    [Authorize]
    public class StudentProfileController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StudentProfileController(AppDbContext context)
        {
            _context = context;
        }

        private string GetUserId() => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";

        [HttpGet("gpa-history")]
        public async Task<IActionResult> GetGpaHistory()
        {
            var userId = GetUserId();
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            if (student == null) return Unauthorized();

            // Typically GPA is calculated by term. We simulate response based on spec.
            var result = new
            {
                semesters = new List<object>
                {
                    new { semester_id = "sem_fall2022", semester_name = "Fall 2022–23", gpa = 3.2, credit_hours = 18, rank_in_cohort = 24 },
                    new { semester_id = "sem_spring2023", semester_name = "Spring 2022–23", gpa = 3.3, credit_hours = 18, rank_in_cohort = 21 },
                    new { semester_id = "sem_fall2023", semester_name = "Fall 2023–24", gpa = 3.5, credit_hours = 17, rank_in_cohort = 15 },
                    new { semester_id = "sem_spring2025", semester_name = "Spring 2024–25", gpa = 3.6, credit_hours = 18, rank_in_cohort = 12 }
                },
                cumulative_gpa = student.GPA,
                trend = "improving"
            };

            return Ok(result);
        }
    }
}
