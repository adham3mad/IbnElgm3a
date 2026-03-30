using IbnElgm3a.DTOs.Audits;
using IbnElgm3a.Enums;
using IbnElgm3a.Model;
using IbnElgm3a.Model.Data;
using IbnElgm3a.Models;
using IbnElgm3a.Filters;
using IbnElgm3a.Services.Localization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IbnElgm3a.Controllers
{
    [ApiController]
    [Route("v1/admin/audit-logs")]
    [Authorize]
    public class AuditLogsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILocalizationService _localizer;

        public AuditLogsController(AppDbContext context, ILocalizationService localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        [HttpGet]
        [RequirePermission(PermissionEnum.Dashboard_AuditLogs_Read)]
        public async Task<IActionResult> GetAuditLogs(
            [FromQuery] string? user_id = null,
            [FromQuery] string? entity = null,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 50)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrEmpty(user_id)) query = query.Where(a => a.UserId == user_id);
            if (!string.IsNullOrEmpty(entity)) query = query.Where(a => a.EntityName == entity);

            var total = await query.CountAsync();
            var logs = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(a => new AuditLogResponseDto
                {
                    Id = 0, // Placeholder as Id is long/int usually, need to check BaseEntity
                    UserId = a.UserId,
                    UserName = a.User != null ? a.User.Name : null,
                    Action = a.Action,
                    EntityName = a.EntityName,
                    EntityId = a.EntityId,
                    Timestamp = a.CreatedAt,
                    Details = $"Old: {a.OldValue}, New: {a.NewValue}",
                    Description = a.Description,
                    Icon = a.Icon
                }).ToListAsync();

            return Ok(ApiResponse<List<AuditLogResponseDto>>.CreateSuccess(logs));
        }
    }
}
