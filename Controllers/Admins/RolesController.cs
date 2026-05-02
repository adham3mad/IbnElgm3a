using IbnElgm3a.DTOs.RolesPermissions;
using IbnElgm3a.Enums;
using IbnElgm3a.Filters;
using IbnElgm3a.Models;
using IbnElgm3a.Models;
using IbnElgm3a.Models.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using IbnElgm3a.Services.Localization;

namespace IbnElgm3a.Controllers.Admins
{
    [ApiController]
    [Route("admin/roles")]
    [Authorize]
    public class RolesController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ILocalizationService _localizer;

        public RolesController(AppDbContext db, ILocalizationService localizer)
        {
            _db = db;
            _localizer = localizer;
        }

        [HttpGet]
        [RequirePermission(PermissionEnum.Dashboard_Roles_Read)]
        public async Task<ActionResult<ApiResponse<List<RoleResponseDto>>>> GetRoles([FromQuery] AppType? type)
        {
            var query = _db.Roles.AsQueryable();
            if (type.HasValue)
                query = query.Where(r => r.Type == type.Value);

            var dbRoles = await query
                .Include(r => r.Permissions)
                    .ThenInclude(p => p.Feature)
                .ToListAsync();

            var roles = dbRoles.Select(r => new RoleResponseDto
            {
                Id = r.Id,
                Name = r.Name,
                NameAr = r.NameAr,
                Description = r.Description,
                Type = r.Type,
                IsActive = r.IsActive,
                Permissions = r.Permissions
                    .GroupBy(p => p.FeatureId)
                    .Select(g => new FeatureResponseDto
                    {
                        Id = g.Key,
                        Name = g.First().Feature.Name,
                        NameAr = g.First().Feature.NameAr,
                        Permissions = g.Select(p => new PermissionResponseDto
                        {
                            Id = p.Id,
                            Name = p.Name,
                            ArName = p.Ar_Name,
                            Description = p.Description,
                            ArDescription = p.Ar_Description
                        }).ToList()
                    }).ToList()
            }).ToList();

            return Ok(ApiResponse<List<RoleResponseDto>>.CreateSuccess(roles));
        }

        [HttpPost]
        [RequirePermission(PermissionEnum.Dashboard_Roles_Create)]
        public async Task<ActionResult<ApiResponse<RoleResponseDto>>> CreateRole(RoleRequestDto request)
        {
            var nameExists = await _db.Roles.AnyAsync(r => r.Name == request.Name || r.NameAr == request.NameAr);
            if (nameExists) return BadRequest(ApiResponse<object>.CreateError("DUPLICATE_NAME", _localizer.GetMessage("DUPLICATE_NAME")));

            var permissions = await _db.Permissions
                .Where(p => request.PermissionIds.Contains(p.Id))
                .ToListAsync();

            var role = new Role
            {
                Name = request.Name,
                NameAr = request.NameAr,
                Description = request.Description,
                Type = request.Type ?? AppType.Platform, // Assuming Platform is a good default or handle as error
                Permissions = permissions
            };

            _db.Roles.Add(role);
            await _db.SaveChangesAsync();

            return Ok(ApiResponse<RoleResponseDto>.CreateSuccess(new RoleResponseDto { Id = role.Id, Name = role.Name }));
        }

        [AllowAnonymous]
        [HttpPut("{id}")]
        [RequirePermission(PermissionEnum.Dashboard_Roles_Update)]
        public async Task<ActionResult<ApiResponse<RoleResponseDto>>> UpdateRole(string id, RoleRequestDto request)
        {
            var role = await _db.Roles.Include(r => r.Permissions).FirstOrDefaultAsync(r => r.Id == id);
            if (role == null) return NotFound(ApiResponse<object>.CreateError("NOT_FOUND", _localizer.GetMessage("ROLE_NOT_FOUND")));

            if (!string.IsNullOrEmpty(request.Name) && request.Name != role.Name)
            {
                if (await _db.Roles.AnyAsync(r => r.Name == request.Name))
                    return BadRequest(ApiResponse<object>.CreateError("DUPLICATE_NAME", _localizer.GetMessage("DUPLICATE_NAME")));
                role.Name = request.Name;
            }
            if (!string.IsNullOrEmpty(request.NameAr) && request.NameAr != role.NameAr)
            {
                if (await _db.Roles.AnyAsync(r => r.NameAr == request.NameAr))
                    return BadRequest(ApiResponse<object>.CreateError("DUPLICATE_NAME", _localizer.GetMessage("DUPLICATE_NAME")));
                role.NameAr = request.NameAr;
            }
            role.Description = request.Description ?? role.Description;
            if (request.Type.HasValue) role.Type = request.Type.Value;

            var permissions = await _db.Permissions
                .Where(p => request.PermissionIds.Contains(p.Id))
                .ToListAsync();

            role.Permissions = permissions;
            await _db.SaveChangesAsync();

            return Ok(ApiResponse<RoleResponseDto>.CreateSuccess(new RoleResponseDto { Id = role.Id, Name = role.Name }));
        }

        [HttpDelete("{id}")]
        [RequirePermission(PermissionEnum.Dashboard_Roles_Delete)]
        public async Task<ActionResult<ApiResponse<object>>> DeleteRole(string id)
        {
            var role = await _db.Roles.FindAsync(id);
            var isawned = await _db.Users.AnyAsync(u => u.RoleId == id);
            if (isawned) return BadRequest(ApiResponse<object>.CreateError("BAD_REQUEST",  _localizer.GetMessage("ROLE_ASSIGNED_TO_USER")));
            if (role == null) return NotFound(ApiResponse<object>.CreateError("NOT_FOUND",  _localizer.GetMessage("ROLE_NOT_FOUND")));

            _db.Roles.Remove(role);
            await _db.SaveChangesAsync();

            return Ok(ApiResponse<object>.CreateSuccess(null!));
        }
    }
}
