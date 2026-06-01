using IbnElgm3a.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System;
using IbnElgm3a.Services;
using IbnElgm3a.Services.Localization;

namespace IbnElgm3a.Controllers.Students
{
    [ApiController]
    [Route("student/announcements")]
    [Authorize(Roles = "student")]
    public class StudentAnnouncementsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILocalizationService _localizer;

        public StudentAnnouncementsController(AppDbContext context, INotificationService notificationService, ILocalizationService localizer)
        {
            _context = context;
            _notificationService = notificationService;
            _localizer = localizer;
        }

        private string GetUserId() => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";

        [HttpGet]
        public async Task<IActionResult> GetAnnouncements([FromQuery] string? type = null, [FromQuery] bool unread_only = false, [FromQuery] int page = 1, [FromQuery] int per_page = 20)
        {
            var userId = GetUserId();

            var studentId = await _context.Students
                .AsNoTracking()
                .Where(s => s.UserId == userId)
                .Select(s => s.Id)
                .FirstOrDefaultAsync();

            if (studentId == null) return Unauthorized();

            var query = _context.Notifications
                .AsNoTracking()
                .Where(n => n.StudentId == studentId && n.Type == "announcement");

            if (unread_only)
            {
                query = query.Where(n => !n.IsRead);
            }

            var total = await query.CountAsync();
            var unreadCount = await _notificationService.GetUnreadCountAsync(studentId);

            var items = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * per_page)
                .Take(per_page)
                .Select(n => new
                {
                    id = n.Id,
                    title = n.Title,
                    body = n.Body,
                    type = n.Type,
                    is_urgent = n.Type == "urgent",
                    sender = "University Admin",
                    target_audience = "all",
                    created_at = n.CreatedAt,
                    is_read = n.IsRead,
                    read_at = n.ReadAt
                })
                .ToListAsync();

            var result = new
            {
                unread_count = unreadCount,
                total = total,
                announcements = items
            };

            return Ok(result);
        }

        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkAnnouncementRead(string id)
        {
            var userId = GetUserId();

            var studentId = await _context.Students
                .AsNoTracking()
                .Where(s => s.UserId == userId)
                .Select(s => s.Id)
                .FirstOrDefaultAsync();

            if (studentId == null) return Unauthorized();

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.StudentId == studentId);

            if (notification == null) return NotFound(new { error = "not_found", message = _localizer.GetMessage("ANNOUNCEMENT_NOT_FOUND") });

            await _notificationService.MarkAsReadAsync(id, studentId);

            return Ok(new
            {
                announcement_id = notification.Id,
                read_at = notification.ReadAt
            });
        }
    }
}
