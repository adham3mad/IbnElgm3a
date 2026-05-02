using IbnElgm3a.DTOs;
using IbnElgm3a.DTOs.RolesPermissions;
using IbnElgm3a.Enums;
using IbnElgm3a.Models;
using IbnElgm3a.Models.Data;
using IbnElgm3a.Models;
using IbnElgm3a.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IbnElgm3a.Controllers.Admins
{
    [ApiController]
    [Route("admin/permissions")]
    [Authorize]
    public class PermissionsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PermissionsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [RequirePermission(PermissionEnum.Dashboard_Permissions_Read)]
        public async Task<IActionResult> GetPermissions()
        {
            var dbPermissions = await _context.Permissions
                .Include(p => p.Feature)
                .OrderBy(p => p.Feature.Name)
                .ThenBy(p => p.Code)
                .ToListAsync();

            var grouped = dbPermissions
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
                }).ToList();

            return Ok(ApiResponse<List<FeatureResponseDto>>.CreateSuccess(grouped));
        }
    }
}
