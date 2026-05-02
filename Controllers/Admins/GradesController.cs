using IbnElgm3a.DTOs.Academics.Grades;
using IbnElgm3a.Enums;
using IbnElgm3a.Models;
using IbnElgm3a.Models.Data;
using IbnElgm3a.Models;
using IbnElgm3a.Filters;
using IbnElgm3a.Services.Localization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IbnElgm3a.Controllers.Admins
{
    [ApiController]
    [Route("admin/grades")]
    [Authorize]
    public class GradesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILocalizationService _localizer;

        public GradesController(AppDbContext context, ILocalizationService localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        [HttpGet]
        [RequirePermission(PermissionEnum.Dashboard_Grades_Read)]
        public async Task<IActionResult> GetGrades([FromQuery] string? enrollment_id = null)
        {
            var query = _context.Grades.AsQueryable();
            if (!string.IsNullOrEmpty(enrollment_id)) query = query.Where(g => g.EnrollmentId == enrollment_id);

            var grades = await query
                .Select(g => new GradeResponseDto
                {
                    Id = g.Id,
                    EnrollmentId = g.EnrollmentId,
                    Marks = g.Marks,
                    LetterGrade = g.LetterGrade,
                    Comments = g.Remarks
                }).ToListAsync();

            return Ok(ApiResponse<List<GradeResponseDto>>.CreateSuccess(grades));
        }

        [HttpGet("{id}")]
        [RequirePermission(PermissionEnum.Dashboard_Grades_Read)]
        public async Task<IActionResult> GetGradeById(string id)
        {
            var g = await _context.Grades.FindAsync(id);
            if (g == null) return NotFound(ApiResponse<object>.CreateError("GRADE_NOT_FOUND", "Grade not found."));

            return Ok(ApiResponse<GradeResponseDto>.CreateSuccess(new GradeResponseDto
            {
                Id = g.Id,
                EnrollmentId = g.EnrollmentId,
                Marks = g.Marks,
                LetterGrade = g.LetterGrade,
                Comments = g.Remarks
            }));
        }

        [HttpPost]
        [RequirePermission(PermissionEnum.Dashboard_Grades_Update)]
        public async Task<IActionResult> CreateGrade([FromBody] CreateGradeRequestDto request)
        {
            var enrollment = await _context.Enrollments.FindAsync(request.EnrollmentId);
            if (enrollment == null) return NotFound(ApiResponse<object>.CreateError("ENROLLMENT_NOT_FOUND", "Enrollment not found."));

            var grade = new Grade
            {
                Id = "grd_" + Guid.NewGuid().ToString("N").Substring(0, 10),
                EnrollmentId = request.EnrollmentId,
                Marks = request.Marks,
                Remarks = request.Comments,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                LastUpdated = DateTimeOffset.UtcNow
            };

            // Basic letter grade calculation (placeholder logic)
            if (grade.Marks >= 90) grade.LetterGrade = LetterGrade.A;
            else if (grade.Marks >= 80) grade.LetterGrade = LetterGrade.B;
            else if (grade.Marks >= 70) grade.LetterGrade = LetterGrade.C;
            else if (grade.Marks >= 60) grade.LetterGrade = LetterGrade.D;
            else grade.LetterGrade = LetterGrade.F;

            _context.Grades.Add(grade);
            await _context.SaveChangesAsync();

            return Created("", ApiResponse<object>.CreateSuccess(new { id = grade.Id }));
        }

        [HttpPatch("{id}")]
        [RequirePermission(PermissionEnum.Dashboard_Grades_Update)]
        public async Task<IActionResult> UpdateGrade(string id, [FromBody] UpdateGradeRequestDto request)
        {
            var g = await _context.Grades.FindAsync(id);
            if (g == null) return NotFound(ApiResponse<object>.CreateError("GRADE_NOT_FOUND", "Grade not found."));

            if (request.Marks.HasValue) 
            {
                g.Marks = request.Marks.Value;
                if (g.Marks >= 90) g.LetterGrade = LetterGrade.A;
                else if (g.Marks >= 80) g.LetterGrade = LetterGrade.B;
                else if (g.Marks >= 70) g.LetterGrade = LetterGrade.C;
                else if (g.Marks >= 60) g.LetterGrade = LetterGrade.D;
                else g.LetterGrade = LetterGrade.F;
            }
            if (request.Comments != null) g.Remarks = request.Comments;
            g.UpdatedAt = DateTimeOffset.UtcNow;
            g.LastUpdated = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.CreateSuccess(new { message = _localizer.GetMessage("UPDATED_SUCCESS") }));
        }

        [HttpDelete("{id}")]
        [RequirePermission(PermissionEnum.Dashboard_Grades_Update)]
        public async Task<IActionResult> DeleteGrade(string id)
        {
            var g = await _context.Grades.FindAsync(id);
            if (g == null) return NotFound(ApiResponse<object>.CreateError("GRADE_NOT_FOUND", "Grade not found."));

            _context.Grades.Remove(g);
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.CreateSuccess(new { message = _localizer.GetMessage("DELETED_SUCCESS") }));
        }
    }
}
