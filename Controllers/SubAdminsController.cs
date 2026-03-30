using IbnElgm3a.Models;
using IbnElgm3a.DTOs.Users;
using IbnElgm3a.DTOs.RolesPermissions;
using IbnElgm3a.DTOs.Dashboard;
using IbnElgm3a.DTOs.SubAdmins;
using Microsoft.AspNetCore.Authorization;
using IbnElgm3a.Filters;
using IbnElgm3a.Enums;
using IbnElgm3a.Services.Localization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using IbnElgm3a.Model;
using IbnElgm3a.Model.Data;
using Microsoft.EntityFrameworkCore;

namespace IbnElgm3a.Controllers
{
    [ApiController]
    [Route("v1/admin/sub-admins")]
    [Authorize]
    public class SubAdminsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILocalizationService _localizer;

        public SubAdminsController(AppDbContext context, ILocalizationService localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        [HttpGet]
        [RequirePermission(PermissionEnum.Dashboard_SubAdmins_Read)]
        public async Task<IActionResult> GetSubAdmins()
        {
            var dbSubAdmins = await _context.SubAdmins
                .Include(s => s.User)
                .ThenInclude(u => u!.Role)
                .ToListAsync();

            var subAdmins = dbSubAdmins.Select(s => new SubAdminListResponseDto
            {
                Id = s.Id,
                FullName = s.User != null ? s.User.Name : "Unknown",
                Email = s.User != null ? s.User.Email : "Unknown",
                Scope = s.ScopeLabel ?? s.ScopeType.ToString(),
                ScopeType = s.ScopeType,
                ScopeId = s.ScopeId ?? string.Empty,
                RoleId = s.User != null ? (s.User.RoleId ?? string.Empty) : string.Empty,
                RoleName = s.User != null && s.User.Role != null ? s.User.Role.Name : "No Role",
                IsActive = s.IsActive,
                LastActiveAt = s.LastActiveAt
            }).ToList();

            return Ok(ApiResponse<List<SubAdminListResponseDto>>.CreateSuccess(subAdmins));
        }

        [HttpPost]
        [RequirePermission(PermissionEnum.Dashboard_SubAdmins_Create)]
        public async Task<IActionResult> CreateSubAdmin([FromBody] CreateSubAdminRequestDto request)
        {
            if (string.IsNullOrEmpty(request.UserId)) return BadRequest(ApiResponse<object>.CreateError("USER_ID_REQUIRED", _localizer.GetMessage("USER_ID_REQUIRED")));

            var user = await _context.Users.FindAsync(request.UserId);
            if (user == null) return NotFound(ApiResponse<object>.CreateError("USER_NOT_FOUND", _localizer.GetMessage("USER_NOT_FOUND")));

            var exists = await _context.SubAdmins.AnyAsync(s => s.UserId == request.UserId);
            if (exists) return BadRequest(ApiResponse<object>.CreateError("SUBADMIN_EXISTS", _localizer.GetMessage("SUBADMIN_EXISTS")));

            {
                var roleExists = await _context.Roles.AnyAsync(r => r.Id == request.RoleId);
                if (!roleExists) return BadRequest(ApiResponse<object>.CreateError("ROLE_NOT_FOUND", _localizer.GetMessage("ROLE_NOT_FOUND")));
            }

            if (request.ScopeType == SubAdminScopeType.Faculty && !string.IsNullOrEmpty(request.ScopeId))
            {
                var facExists = await _context.Faculties.AnyAsync(f => f.Id == request.ScopeId);
                if (!facExists) return BadRequest(ApiResponse<object>.CreateError("FACULTY_NOT_FOUND", _localizer.GetMessage("FACULTY_NOT_FOUND")));
            }
            else if (request.ScopeType == SubAdminScopeType.Department && !string.IsNullOrEmpty(request.ScopeId))
            {
                var depExists = await _context.Departments.AnyAsync(d => d.Id == request.ScopeId);
                if (!depExists) return BadRequest(ApiResponse<object>.CreateError("DEPARTMENT_NOT_FOUND", _localizer.GetMessage("DEPARTMENT_NOT_FOUND")));
            }
            
            var subAdmin = new SubAdmin
            {
                Id = "sub_" + System.Guid.NewGuid().ToString("N").Substring(0, 10),
                UserId = request.UserId,
                ScopeType = request.ScopeType,
                ScopeId = request.ScopeId,
                IsActive = true
            };

            // Update user role
            if (!string.IsNullOrEmpty(request.RoleId))
            {
                user.RoleId = request.RoleId;
            }
            
            _context.SubAdmins.Add(subAdmin);
            await _context.SaveChangesAsync();
            
            return Created("", ApiResponse<object>.CreateSuccess(new { id = subAdmin.Id }));
        }

        [HttpPatch("{sub_admin_id}")]
        [RequirePermission(PermissionEnum.Dashboard_SubAdmins_Update)]
        public async Task<IActionResult> UpdateSubAdmin(string sub_admin_id, [FromBody] UpdateSubAdminRequestDto request)
        {
            var subAdmin = await _context.SubAdmins.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == sub_admin_id);
            if (subAdmin == null) return NotFound(ApiResponse<object>.CreateError("SUBADMIN_NOT_FOUND", _localizer.GetMessage("SUBADMIN_NOT_FOUND")));

            if (request.IsActive.HasValue) subAdmin.IsActive = request.IsActive.Value;
            if (request.RoleId != null)
            {
                var roleExists = await _context.Roles.AnyAsync(r => r.Id == request.RoleId);
                if (!roleExists) return BadRequest(ApiResponse<object>.CreateError("ROLE_NOT_FOUND", _localizer.GetMessage("ROLE_NOT_FOUND")));
                
                if (subAdmin.User != null) subAdmin.User.RoleId = request.RoleId;
            }

            if (request.ScopeType.HasValue || request.ScopeId != null)
            {
                var targetType = request.ScopeType ?? subAdmin.ScopeType;
                var targetId = request.ScopeId ?? subAdmin.ScopeId;

                if (targetType == SubAdminScopeType.Faculty && !string.IsNullOrEmpty(targetId))
                {
                    var facExists = await _context.Faculties.AnyAsync(f => f.Id == targetId);
                    if (!facExists) return BadRequest(ApiResponse<object>.CreateError("FACULTY_NOT_FOUND", _localizer.GetMessage("FACULTY_NOT_FOUND")));
                }
                else if (targetType == SubAdminScopeType.Department && !string.IsNullOrEmpty(targetId))
                {
                    var depExists = await _context.Departments.AnyAsync(d => d.Id == targetId);
                    if (!depExists) return BadRequest(ApiResponse<object>.CreateError("DEPARTMENT_NOT_FOUND", _localizer.GetMessage("DEPARTMENT_NOT_FOUND")));
                }

                if (request.ScopeType.HasValue) subAdmin.ScopeType = request.ScopeType.Value;
                if (request.ScopeId != null) subAdmin.ScopeId = request.ScopeId;
            }

            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.CreateSuccess(new { message = _localizer.GetMessage("UPDATED_SUCCESS") }));
        }

        [HttpDelete("{sub_admin_id}")]
        [RequirePermission(PermissionEnum.Dashboard_SubAdmins_Delete)]
        public async Task<IActionResult> DeleteSubAdmin(string sub_admin_id)
        {
            var subAdmin = await _context.SubAdmins.FindAsync(sub_admin_id);
            if (subAdmin == null) return NotFound(ApiResponse<object>.CreateError("SUBADMIN_NOT_FOUND", _localizer.GetMessage("SUBADMIN_NOT_FOUND")));

            _context.SubAdmins.Remove(subAdmin);
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.CreateSuccess(new { message = _localizer.GetMessage("DELETED_SUCCESS") }));
        }

        [HttpGet("{sub_admin_id}/activity")]
        [RequirePermission(PermissionEnum.Dashboard_SubAdmins_Read)]
        public async Task<IActionResult> GetSubAdminActivity(string sub_admin_id, [FromQuery] int page = 1, [FromQuery] int limit = 30)
        {
            var subAdmin = await _context.SubAdmins.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == sub_admin_id);
            if (subAdmin == null) return NotFound(ApiResponse<object>.CreateError("SUBADMIN_NOT_FOUND", _localizer.GetMessage("SUBADMIN_NOT_FOUND")));

            var query = _context.AuditLogs.Where(a => a.UserId == subAdmin.UserId);

            var total = await query.CountAsync();
            var logs = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(a => new ActivityDto
                {
                    Icon = a.Icon ?? (a.Action == "Update" ? "edit" : (a.Action == "Create" ? "plus" : "activity")),
                    Text = a.Description ?? $"{a.Action} on {a.EntityName}",
                    PerformedAt = a.CreatedAt,
                    ActorName = subAdmin.User != null ? subAdmin.User.Name : "Sub-Admin"
                }).ToListAsync();

            var pag = new ApiPagination { Page = page, Limit = limit, Total = total, HasMore = (page * limit) < total };
            return Ok(ApiResponse<List<ActivityDto>>.CreateSuccess(logs, pagination: pag));
        }
    }
}
