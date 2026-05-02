using IbnElgm3a.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using IbnElgm3a.Services;

namespace IbnElgm3a.Controllers.Students
{
    [ApiController]
    [Route("student/notifications")]
    [Authorize(Roles = "student")]
    public class StudentNotificationsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IbnElgm3a.Services.Localization.ILocalizationService _localizer;

        public StudentNotificationsController(AppDbContext context, INotificationService notificationService, IbnElgm3a.Services.Localization.ILocalizationService localizer)
        {
            _context = context;
            _notificationService = notificationService;
            _localizer = localizer;
        }

        private string GetUserId() => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";

        [HttpGet]
        public async Task<IActionResult> GetNotifications([FromQuery] string? type = null, [FromQuery] bool unread_only = false, [FromQuery] int page = 1, [FromQuery] int per_page = 20)
        {
            var userId = GetUserId();
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            if (student == null) return Unauthorized();

            var query = _context.Notifications.Where(n => n.StudentId == student.Id).AsQueryable();

            if (!string.IsNullOrEmpty(type))
            {
                query = query.Where(n => n.Type == type);
            }
            if (unread_only)
            {
                query = query.Where(n => !n.IsRead);
            }

            var unreadCount = await _notificationService.GetUnreadCountAsync(student.Id);
            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * per_page)
                .Take(per_page)
                .ToListAsync();

            return Ok(new
            {
                unread_count = unreadCount,
                notifications = items.Select(n => new
                {
                    id = n.Id,
                    type = n.Type,
                    title = n.Title,
                    body = n.Body,
                    is_read = n.IsRead,
                    created_at = n.CreatedAt,
                    action_url = n.ActionUrl
                }).ToList()
            });
        }

        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkRead(string id)
        {
            var userId = GetUserId();
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            if (student == null) return Unauthorized();

            var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.StudentId == student.Id);
            if (notification == null) return NotFound(new { error = "not_found", message = _localizer.GetMessage("NOTIFICATION_NOT_FOUND") });

            await _notificationService.MarkAsReadAsync(id, student.Id);
            var unreadCount = await _notificationService.GetUnreadCountAsync(student.Id);

            return Ok(new
            {
                notification_id = notification.Id,
                is_read = true,
                read_at = notification.ReadAt,
                remaining_unread_count = unreadCount
            });
        }

        [HttpPost("read-all")]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = GetUserId();
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            if (student == null) return Unauthorized();

            await _notificationService.MarkAllAsReadAsync(student.Id);

            return Ok(new { message = _localizer.GetMessage("NOTIFICATIONS_MARKED_READ") });
        }
    }
}
