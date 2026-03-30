using IbnElgm3a.DTOs.Guardians;
using IbnElgm3a.DTOs.Common;
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

namespace IbnElgm3a.Controllers
{
    [ApiController]
    [Route("v1/admin/guardians")]
    [Authorize]
    public class GuardiansController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILocalizationService _localizer;

        public GuardiansController(AppDbContext context, ILocalizationService localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        [HttpGet]
        [RequirePermission(PermissionEnum.Dashboard_Guardians_Read)]
        public async Task<IActionResult> GetGuardians([FromQuery] string? q = null)
        {
            var query = _context.Guardians.AsQueryable();
            if (!string.IsNullOrEmpty(q))
            {
                var qLower = q.ToLower();
                query = query.Where(g => g.FullName.ToLower().Contains(qLower) || g.NationalId == q || g.Phone == q);
            }

            var guardians = await query
                .Select(g => new GuardianResponseDto
                {
                    Id = g.Id,
                    FullName = g.FullName,
                    NationalId = g.NationalId,
                    Phone = g.Phone,
                    Email = g.Email,
                    Address = g.Address,
                    Job = g.Job
                }).ToListAsync();

            return Ok(ApiResponse<List<GuardianResponseDto>>.CreateSuccess(guardians));
        }

        [HttpGet("{id}")]
        [RequirePermission(PermissionEnum.Dashboard_Guardians_Read)]
        public async Task<IActionResult> GetGuardianById(string id)
        {
            var g = await _context.Guardians.FindAsync(id);
            if (g == null) return NotFound(ApiResponse<object>.CreateError("GUARDIAN_NOT_FOUND", "Guardian not found."));

            return Ok(ApiResponse<GuardianResponseDto>.CreateSuccess(new GuardianResponseDto
            {
                Id = g.Id,
                FullName = g.FullName,
                NationalId = g.NationalId,
                Phone = g.Phone,
                Email = g.Email,
                Address = g.Address,
                Job = g.Job
            }));
        }

        [HttpPost]
        [RequirePermission(PermissionEnum.Dashboard_Guardians_Read)]
        public async Task<IActionResult> CreateGuardian([FromBody] CreateGuardianRequestDto request)
        {
            var nidExists = await _context.Guardians.AnyAsync(g => g.NationalId == request.NationalId);
            if (nidExists) return BadRequest(ApiResponse<object>.CreateError("DUPLICATE_NATIONAL_ID", "Guardian with this National ID already exists."));

            var guardian = new Guardian
            {
                Id = "gdn_" + Guid.NewGuid().ToString("N").Substring(0, 12),
                FullName = request.FullName,
                NationalId = request.NationalId,
                Phone = request.Phone,
                Email = request.Email ?? "",
                Address = request.Address ?? "",
                Job = request.Job ?? ""
            };

            _context.Guardians.Add(guardian);
            await _context.SaveChangesAsync();

            return Created("", ApiResponse<object>.CreateSuccess(new { id = guardian.Id }));
        }

        [HttpPatch("{id}")]
        [RequirePermission(PermissionEnum.Dashboard_Guardians_Update)]
        public async Task<IActionResult> UpdateGuardian(string id, [FromBody] UpdateGuardianRequestDto request)
        {
            var g = await _context.Guardians.FindAsync(id);
            if (g == null) return NotFound(ApiResponse<object>.CreateError("GUARDIAN_NOT_FOUND", "Guardian not found."));

            if (request.FullName != null) g.FullName = request.FullName;
            if (request.NationalId != null && request.NationalId != g.NationalId)
            {
                if (await _context.Guardians.AnyAsync(other => other.NationalId == request.NationalId))
                    return BadRequest(ApiResponse<object>.CreateError("DUPLICATE_NATIONAL_ID", "Another guardian has this National ID."));
                g.NationalId = request.NationalId;
            }
            if (request.Phone != null) g.Phone = request.Phone;
            if (request.Email != null) g.Email = request.Email;
            if (request.Address != null) g.Address = request.Address;
            if (request.Job != null) g.Job = request.Job;

            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.CreateSuccess(new { message = _localizer.GetMessage("UPDATED_SUCCESS") }));
        }

        [HttpDelete("{id}")]
        [RequirePermission(PermissionEnum.Dashboard_Guardians_Delete)]
        public async Task<IActionResult> DeleteGuardian(string id)
        {
            var g = await _context.Guardians.FindAsync(id);
            if (g == null) return NotFound(ApiResponse<object>.CreateError("GUARDIAN_NOT_FOUND", "Guardian not found."));

            _context.Guardians.Remove(g);
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.CreateSuccess(new { message = _localizer.GetMessage("DELETED_SUCCESS") }));
        }
    }
}
