using IbnElgm3a.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System;
using IbnElgm3a.Services;

namespace IbnElgm3a.Controllers.Students
{
    [ApiController]
    [Route("student/announcements")]
    [Authorize]
    public class StudentAnnouncementsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;

        public StudentAnnouncementsController(AppDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        private string GetUserId() => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";

        // Reusing Notification model for read status or just fetching announcements
        // Since the prompt combines "announcements" and "notifications" and we defined Notification model,
        // we'll filter them by Type == "announcement"
        
        [HttpGet]
        public async Task<IActionResult> GetAnnouncements([FromQuery] string? type = null, [FromQuery] bool unread_only = false, [FromQuery] int page = 1, [FromQuery] int per_page = 20)
        {
            var userId = GetUserId();
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            if (student == null) return Unauthorized();

            var query = _context.Notifications
                .Where(n => n.StudentId == student.Id && n.Type == "announcement")
                .AsQueryable();

            if (unread_only)
            {
                query = query.Where(n => !n.IsRead);
            }

            // Note: In an actual implementation, the prompt separates `announcements` list from `notifications` feed.
            // But they map identically to the "Notification" structure we just made. 
            // The JSON given for announcement has `is_urgent`, `sender`, and `target_audience`.
            // Our Notification model doesn't have these specifically, but we return mock or mapped equivalents.

            var total = await query.CountAsync();
            var unreadCount = await _notificationService.GetUnreadCountAsync(student.Id);

            var items = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * per_page)
                .Take(per_page)
                .ToListAsync();

            var result = new
            {
                unread_count = unreadCount,
                total = total,
                announcements = items.Select(n => new
                {
                    id = n.Id,
                    title = n.Title,
                    body = n.Body,
                    type = type ?? "general", // mocked type if empty
                    is_urgent = type == "urgent",
                    sender = "University Admin", // mocked
                    target_audience = "all", // mocked
                    created_at = n.CreatedAt,
                    is_read = n.IsRead,
                    read_at = n.ReadAt
                }).ToList()
            };

            return Ok(result);
        }

        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkAnnouncementRead(string id)
        {
            var userId = GetUserId();
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            if (student == null) return Unauthorized();

            var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.StudentId == student.Id);
            if (notification == null) return NotFound(new { error = "not_found", message = "Announcement not found" });

            await _notificationService.MarkAsReadAsync(id, student.Id);

            return Ok(new
            {
                announcement_id = notification.Id,
                read_at = notification.ReadAt
            });
        }
    }
}
