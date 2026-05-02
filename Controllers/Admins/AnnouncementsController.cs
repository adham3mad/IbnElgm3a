using IbnElgm3a.DTOs.Announcements;
using IbnElgm3a.DTOs.Common;
using IbnElgm3a.Models;
using IbnElgm3a.Enums;
using IbnElgm3a.Filters;
using IbnElgm3a.Services.Localization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using IbnElgm3a.Models;
using IbnElgm3a.Models.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace IbnElgm3a.Controllers.Admins
{
    [ApiController]
    [Route("admin/announcements")]
    [Authorize]
    public class AnnouncementsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILocalizationService _localizer;
    
        public AnnouncementsController(AppDbContext context, ILocalizationService localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        [HttpGet]
        [RequirePermission(PermissionEnum.Dashboard_Announcements_Read)]
        public async Task<IActionResult> GetAnnouncements(
            [FromQuery] AnnouncementTargetType? target = null,
            [FromQuery] AnnouncementPriority? priority = null,
            [FromQuery] string? q = null,
            [FromQuery] int page = 1, 
            [FromQuery] int limit = 20)
        {
            var query = _context.Announcements
                .Include(a => a.CreatedBy)
                .AsQueryable();

            if (target.HasValue) query = query.Where(a => a.TargetType == target.Value);
            if (priority.HasValue) query = query.Where(a => a.Priority == priority.Value);
            if (!string.IsNullOrEmpty(q))
            {
                var qLower = q.ToLower();
                query = query.Where(a => a.Title.ToLower().Contains(qLower) || a.Body.ToLower().Contains(qLower));
            }

            var total = await query.CountAsync();
            var announcements = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(a => new AnnouncementListResponseDto
                {
                    Id = a.Id,
                    Title = a.Title,
                    Body = a.Body,
                    TargetType = a.TargetType,
                    TargetLabel = a.TargetLabel ?? a.TargetType.ToString(),
                    SentCount = a.SentCount,
                    ReadCount = a.ReadCount,
                    CreatedBy = a.CreatedBy != null ? new IdNameDto { Id = a.CreatedById, Name = a.CreatedBy.Name } : null,
                    CreatedAt = a.CreatedAt
                }).ToListAsync();

            var pag = new ApiPagination { Page = page, Limit = limit, Total = total, HasMore = (page * limit) < total };
            return Ok(ApiResponse<List<AnnouncementListResponseDto>>.CreateSuccess(announcements, pagination: pag));
        }

        [HttpPost]
        [RequirePermission(PermissionEnum.Dashboard_Announcements_Create)]
        public async Task<IActionResult> CreateAnnouncement([FromBody] CreateAnnouncementRequestDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            var announcement = new Announcement
            {
                Id = "ann_" + System.Guid.NewGuid().ToString("N").Substring(0, 10),
                Title = request.Title,
                Body = request.Body,
                TargetType = request.TargetType,
                TargetLabel = request.TargetType.ToString(),
                Priority = request.Priority,
                CreatedById = userId ?? "system"
            };

            _context.Announcements.Add(announcement);
            await _context.SaveChangesAsync();
            
            // Background logic simulation for sent count
            var sentCount = await _context.Users.CountAsync(u => u.Status == IbnElgm3a.Enums.UserStatus.Active);
            announcement.SentCount = sentCount;
            await _context.SaveChangesAsync();

            return Created("", ApiResponse<object>.CreateSuccess(new { id = announcement.Id, sent_count = sentCount }));
        }

        [HttpPatch("{announcement_id}")]
        [RequirePermission(PermissionEnum.Dashboard_Announcements_Update)]
        public async Task<IActionResult> UpdateAnnouncement(string announcement_id, [FromBody] UpdateAnnouncementRequestDto request)
        {
            var a = await _context.Announcements.FindAsync(announcement_id);
            if (a == null) return NotFound(ApiResponse<object>.CreateError("ANNOUNCEMENT_NOT_FOUND", _localizer.GetMessage("ANNOUNCEMENT_NOT_FOUND")));

            if (!string.IsNullOrEmpty(request.Title)) a.Title = request.Title;
            if (!string.IsNullOrEmpty(request.Body)) a.Body = request.Body;
            if (request.TargetType.HasValue) a.TargetType = request.TargetType.Value;
            if (request.TargetId != null) a.TargetId = request.TargetId;
            if (request.TargetRole != null) a.TargetRole = request.TargetRole;
            if (request.Priority.HasValue) a.Priority = request.Priority.Value;

            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.CreateSuccess(new { message = _localizer.GetMessage("ANNOUNCEMENT_UPDATED") }));
        }

        [HttpDelete("{announcement_id}")]
        [RequirePermission(PermissionEnum.Dashboard_Announcements_Delete)]
        public async Task<IActionResult> DeleteAnnouncement(string announcement_id)
        {
            var a = await _context.Announcements.FindAsync(announcement_id);
            if (a == null) return NotFound(ApiResponse<object>.CreateError("ANNOUNCEMENT_NOT_FOUND", _localizer.GetMessage("ANNOUNCEMENT_NOT_FOUND")));

            _context.Announcements.Remove(a);
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.CreateSuccess(new { message = _localizer.GetMessage("ANNOUNCEMENT_DELETED") }));
        }
    }
}
