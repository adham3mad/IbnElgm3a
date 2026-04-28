using IbnElgm3a.DTOs.RolesPermissions;
using IbnElgm3a.Enums;
using IbnElgm3a.Models;
using IbnElgm3a.Models;
using IbnElgm3a.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IbnElgm3a.Services.Localization;

namespace IbnElgm3a.Controllers.Admins
{
    [ApiController]
    [Route("api/admin/features")]
    [Authorize]
    public class FeaturesController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ILocalizationService _localizer;

        public FeaturesController(AppDbContext db, ILocalizationService localizer)
        {
            _db = db;
            _localizer = localizer;
        }

        [HttpGet]
        [RequirePermission(PermissionEnum.Dashboard_Features_Read)]
        public async Task<ActionResult<ApiResponse<List<FeatureResponseDto>>>> GetFeatures([FromQuery] AppType? type)
        {
            var query = _db.Features.AsQueryable();
            if (type.HasValue)
                query = query.Where(f => f.Type == type.Value);

            var features = await query
                .Include(f => f.Permissions)
                .Select(f => new FeatureResponseDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    NameAr = f.NameAr,
                    Type = f.Type,
                    Permissions = f.Permissions.Select(p => new PermissionResponseDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        ArName = p.Ar_Name,
                        Description = p.Description,
                        ArDescription = p.Ar_Description
                    }).ToList()
                }).ToListAsync();

            return Ok(ApiResponse<List<FeatureResponseDto>>.CreateSuccess(features));
        }
    }
}
