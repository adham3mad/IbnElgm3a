using IbnElgm3a.DTOs;
using IbnElgm3a.Enums;
using IbnElgm3a.Model;
using IbnElgm3a.Model.Data;
using IbnElgm3a.Models;
using IbnElgm3a.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IbnElgm3a.Controllers
{
    [ApiController]
    [Route("v1/admin/permissions")]
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
            var permissions = await _context.Permissions
                .OrderBy(p => p.Code)
                .Select(p => new 
                {
                    id = p.Id,
                    code = p.Code,
                    name = p.Name,
                    description = p.Description
                }).ToListAsync();

            return Ok(ApiResponse<object>.CreateSuccess(permissions));
        }
    }
}
