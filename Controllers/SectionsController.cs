using IbnElgm3a.DTOs.Academics.Sections;
using IbnElgm3a.Enums;
using IbnElgm3a.Model;
using IbnElgm3a.Model.Data;
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
    [Route("v1/admin/sections")]
    [Authorize]
    public class SectionsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILocalizationService _localizer;

        public SectionsController(AppDbContext context, ILocalizationService localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        [HttpGet]
        [RequirePermission(PermissionEnum.Dashboard_Sections_Read)]
        public async Task<IActionResult> GetSections([FromQuery] string? course_id = null)
        {
            var query = _context.Sections.AsQueryable();
            if (!string.IsNullOrEmpty(course_id)) query = query.Where(s => s.CourseId == course_id);

            var sections = await query
                .Select(s => new SectionResponseDto
                {
                    Id = s.Id,
                    CourseId = s.CourseId,
                    InstructorId = s.InstructorId,
                    RoomId = s.Room, // Models uses 'Room' string, need to check if it should be RoomId
                    Capacity = s.Capacity,
                    EnrolledCount = s.Enrollments.Count
                }).ToListAsync();

            return Ok(ApiResponse<List<SectionResponseDto>>.CreateSuccess(sections));
        }

        [HttpGet("{id}")]
        [RequirePermission(PermissionEnum.Dashboard_Sections_Read)]
        public async Task<IActionResult> GetSectionById(string id)
        {
            var s = await _context.Sections.Include(sec => sec.Enrollments).FirstOrDefaultAsync(sec => sec.Id == id);
            if (s == null) return NotFound(ApiResponse<object>.CreateError("SECTION_NOT_FOUND", "Section not found."));

            return Ok(ApiResponse<SectionResponseDto>.CreateSuccess(new SectionResponseDto
            {
                Id = s.Id,
                CourseId = s.CourseId,
                InstructorId = s.InstructorId,
                RoomId = s.Room,
                Capacity = s.Capacity,
                EnrolledCount = s.Enrollments.Count
            }));
        }

        [HttpPost]
        [RequirePermission(PermissionEnum.Dashboard_Sections_Create)]
        public async Task<IActionResult> CreateSection([FromBody] CreateSectionRequestDto request)
        {
            var section = new Section
            {
                Id = "sec_" + Guid.NewGuid().ToString("N").Substring(0, 10),
                CourseId = request.CourseId,
                InstructorId = request.InstructorId,
                Room = request.RoomId ?? "",
                Capacity = request.Capacity,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            _context.Sections.Add(section);
            await _context.SaveChangesAsync();

            return Created("", ApiResponse<object>.CreateSuccess(new { id = section.Id }));
        }

        [HttpPatch("{id}")]
        [RequirePermission(PermissionEnum.Dashboard_Sections_Update)]
        public async Task<IActionResult> UpdateSection(string id, [FromBody] UpdateSectionRequestDto request)
        {
            var s = await _context.Sections.FindAsync(id);
            if (s == null) return NotFound(ApiResponse<object>.CreateError("SECTION_NOT_FOUND", "Section not found."));

            if (request.InstructorId != null) s.InstructorId = request.InstructorId;
            if (request.RoomId != null) s.Room = request.RoomId;
            if (request.Capacity.HasValue) s.Capacity = request.Capacity.Value;
            s.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.CreateSuccess(new { message = _localizer.GetMessage("UPDATED_SUCCESS") }));
        }

        [HttpDelete("{id}")]
        [RequirePermission(PermissionEnum.Dashboard_Sections_Delete)]
        public async Task<IActionResult> DeleteSection(string id)
        {
            var s = await _context.Sections.FindAsync(id);
            if (s == null) return NotFound(ApiResponse<object>.CreateError("SECTION_NOT_FOUND", "Section not found."));

            if (await _context.Enrollments.AnyAsync(e => e.SectionId == id))
                return BadRequest(ApiResponse<object>.CreateError("SECTION_NOT_EMPTY", "Section has active enrollments."));

            _context.Sections.Remove(s);
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.CreateSuccess(new { message = _localizer.GetMessage("DELETED_SUCCESS") }));
        }
    }
}
