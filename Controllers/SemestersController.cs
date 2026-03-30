using IbnElgm3a.DTOs.Academics.Semesters;
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
    [Route("v1/admin/semesters")]
    [Authorize]
    public class SemestersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILocalizationService _localizer;

        public SemestersController(AppDbContext context, ILocalizationService localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        [HttpGet]
        [RequirePermission(PermissionEnum.Dashboard_Semesters_Read)]
        public async Task<IActionResult> GetSemesters()
        {
            var semesters = await _context.Semesters
                .OrderByDescending(s => s.StartDate)
                .Select(s => new SemesterResponseDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    StartDate = s.StartDate,
                    EndDate = s.EndDate
                }).ToListAsync();

            return Ok(ApiResponse<List<SemesterResponseDto>>.CreateSuccess(semesters));
        }

        [HttpGet("{id}")]
        [RequirePermission(PermissionEnum.Dashboard_Semesters_Read)]
        public async Task<IActionResult> GetSemesterById(string id)
        {
            var s = await _context.Semesters.FindAsync(id);
            if (s == null) return NotFound(ApiResponse<object>.CreateError("SEMESTER_NOT_FOUND", _localizer.GetMessage("SEMESTER_NOT_FOUND")));

            return Ok(ApiResponse<SemesterResponseDto>.CreateSuccess(new SemesterResponseDto
            {
                Id = s.Id,
                Name = s.Name,
                StartDate = s.StartDate,
                EndDate = s.EndDate
            }));
        }

        [HttpPost]
        [RequirePermission(PermissionEnum.Dashboard_Semesters_Create)]
        public async Task<IActionResult> CreateSemester([FromBody] CreateSemesterRequestDto request)
        {
            var semester = new Semester
            {
                Id = "sem_" + Guid.NewGuid().ToString("N").Substring(0, 10),
                Name = request.Name,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            _context.Semesters.Add(semester);
            await _context.SaveChangesAsync();

            return Created("", ApiResponse<object>.CreateSuccess(new { id = semester.Id }));
        }

        [HttpPatch("{id}")]
        [RequirePermission(PermissionEnum.Dashboard_Semesters_Update)]
        public async Task<IActionResult> UpdateSemester(string id, [FromBody] UpdateSemesterRequestDto request)
        {
            var s = await _context.Semesters.FindAsync(id);
            if (s == null) return NotFound(ApiResponse<object>.CreateError("SEMESTER_NOT_FOUND", _localizer.GetMessage("SEMESTER_NOT_FOUND")));

            if (request.Name != null) s.Name = request.Name;
            if (request.StartDate.HasValue) s.StartDate = request.StartDate.Value;
            if (request.EndDate.HasValue) s.EndDate = request.EndDate.Value;
            s.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.CreateSuccess(new { message = _localizer.GetMessage("UPDATED_SUCCESS") }));
        }

        [HttpDelete("{id}")]
        [RequirePermission(PermissionEnum.Dashboard_Semesters_Delete)]
        public async Task<IActionResult> DeleteSemester(string id)
        {
            var s = await _context.Semesters.FindAsync(id);
            if (s == null) return NotFound(ApiResponse<object>.CreateError("SEMESTER_NOT_FOUND", _localizer.GetMessage("SEMESTER_NOT_FOUND")));

            // Check if used in courses or exams
            if (await _context.Courses.AnyAsync(c => c.SemesterId == id))
                return BadRequest(ApiResponse<object>.CreateError("SEMESTER_IN_USE", "Semester is in use by courses."));

            _context.Semesters.Remove(s);
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.CreateSuccess(new { message = _localizer.GetMessage("DELETED_SUCCESS") }));
        }
    }
}
