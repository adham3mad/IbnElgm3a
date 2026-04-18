using IbnElgm3a.DTOs.Academics.Enrollments;
using IbnElgm3a.Enums;
using IbnElgm3a.Models;
using IbnElgm3a.Models.Data;
using IbnElgm3a.Models;
using IbnElgm3a.Models.Data;
using IbnElgm3a.Filters;
using IbnElgm3a.Services.Localization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IbnElgm3a.Controllers
{
    [ApiController]
    [Route("v1/admin/enrollments")]
    [Authorize]
    public class EnrollmentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILocalizationService _localizer;

        public EnrollmentsController(AppDbContext context, ILocalizationService localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        [HttpGet]
        [RequirePermission(PermissionEnum.Dashboard_Enrollments_Read)]
        public async Task<IActionResult> GetEnrollments([FromQuery] string? student_id = null, [FromQuery] string? section_id = null)
        {
            var query = _context.Enrollments.AsQueryable();
            if (!string.IsNullOrEmpty(student_id)) query = query.Where(e => e.StudentId == student_id);
            if (!string.IsNullOrEmpty(section_id)) query = query.Where(e => e.SectionId == section_id);

            var enrollments = await query
                .Select(e => new EnrollmentResponseDto
                {
                    Id = e.Id,
                    StudentId = e.StudentId,
                    SectionId = e.SectionId,
                    Status = e.Status,
                    EnrolledAt = e.EnrolledAt
                }).ToListAsync();

            return Ok(ApiResponse<List<EnrollmentResponseDto>>.CreateSuccess(enrollments));
        }

        [HttpGet("{id}")]
        [RequirePermission(PermissionEnum.Dashboard_Enrollments_Read)]
        public async Task<IActionResult> GetEnrollmentById(string id)
        {
            var e = await _context.Enrollments.FindAsync(id);
            if (e == null) return NotFound(ApiResponse<object>.CreateError("ENROLLMENT_NOT_FOUND", "Enrollment not found."));

            return Ok(ApiResponse<EnrollmentResponseDto>.CreateSuccess(new EnrollmentResponseDto
            {
                Id = e.Id,
                StudentId = e.StudentId,
                SectionId = e.SectionId,
                Status = e.Status,
                EnrolledAt = e.EnrolledAt
            }));
        }

        [HttpPost]
        [RequirePermission(PermissionEnum.Dashboard_Enrollments_Create)]
        public async Task<IActionResult> CreateEnrollment([FromBody] CreateEnrollmentRequestDto request)
        {
            var section = await _context.Sections.Include(s => s.Enrollments).FirstOrDefaultAsync(s => s.Id == request.SectionId);
            if (section == null) return NotFound(ApiResponse<object>.CreateError("SECTION_NOT_FOUND", "Section not found."));

            if (section.Enrollments.Count >= section.Capacity)
                return BadRequest(ApiResponse<object>.CreateError("SECTION_FULL", "Section is at full capacity."));

            var exists = await _context.Enrollments.AnyAsync(e => e.StudentId == request.StudentId && e.SectionId == request.SectionId);
            if (exists) return BadRequest(ApiResponse<object>.CreateError("ALREADY_ENROLLED", "Student is already enrolled in this section."));

            var enrollment = new Enrollment
            {
                Id = "enr_" + Guid.NewGuid().ToString("N").Substring(0, 10),
                StudentId = request.StudentId,
                SectionId = request.SectionId,
                Status = request.Status,
                EnrolledAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            _context.Enrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            return Created("", ApiResponse<object>.CreateSuccess(new { id = enrollment.Id }));
        }

        [HttpPatch("{id}")]
        [RequirePermission(PermissionEnum.Dashboard_Enrollments_Update)]
        public async Task<IActionResult> UpdateEnrollment(string id, [FromBody] UpdateEnrollmentRequestDto request)
        {
            var e = await _context.Enrollments.FindAsync(id);
            if (e == null) return NotFound(ApiResponse<object>.CreateError("ENROLLMENT_NOT_FOUND", "Enrollment not found."));

            if (request.Status.HasValue) e.Status = request.Status.Value;
            e.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.CreateSuccess(new { message = _localizer.GetMessage("UPDATED_SUCCESS") }));
        }

        [HttpDelete("{id}")]
        [RequirePermission(PermissionEnum.Dashboard_Enrollments_Delete)]
        public async Task<IActionResult> DeleteEnrollment(string id)
        {
            var e = await _context.Enrollments.FindAsync(id);
            if (e == null) return NotFound(ApiResponse<object>.CreateError("ENROLLMENT_NOT_FOUND", "Enrollment not found."));

            _context.Enrollments.Remove(e);
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.CreateSuccess(new { message = _localizer.GetMessage("DELETED_SUCCESS") }));
        }
    }
}
