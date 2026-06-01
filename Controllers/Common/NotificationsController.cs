using IbnElgm3a.Models;
using IbnElgm3a.Models.Data;
using IbnElgm3a.Enums;
using IbnElgm3a.Services;
using IbnElgm3a.Services.Localization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace IbnElgm3a.Controllers.Common
{
    [ApiController]
    [Route("notifications")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILocalizationService _localizer;

        public NotificationsController(AppDbContext context, INotificationService notificationService, ILocalizationService localizer)
        {
            _context = context;
            _notificationService = notificationService;
            _localizer = localizer;
        }

        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        private bool IsStudent() => User.IsInRole("student");

        [HttpGet]
        public async Task<IActionResult> GetNotifications(
            [FromQuery] NotificationType? type = null, 
            [FromQuery] bool unread_only = false, 
            [FromQuery] int page = 1, 
            [FromQuery] int per_page = 20,
            [FromQuery] int? limit = null)
        {
            var userId = GetUserId();
            var isStudent = IsStudent();
            
            // Map limit to per_page for backward compatibility
            var actualPerPage = limit ?? per_page;

            if (isStudent)
            {
                var student = await _context.Students.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == userId);
                if (student == null) return Unauthorized();

                var query = _context.Notifications.AsNoTracking().Where(n => n.StudentId == student.Id).AsQueryable();

                if (type.HasValue)
                {
                    // Compute the filter value as a constant string - PostgreSQL can translate this correctly
                    var typeStr = type.Value.ToString().ToLower();
                    query = query.Where(n => n.Type == typeStr);
                }
                if (unread_only)
                {
                    query = query.Where(n => !n.IsRead);
                }

                var unreadCount = await _notificationService.GetUnreadCountAsync(student.Id, isStudent: true);

                var items = await query
                    .OrderByDescending(n => n.CreatedAt)
                    .Skip((page - 1) * actualPerPage)
                    .Take(actualPerPage)
                    .Select(n => new
                    {
                        id = n.Id,
                        type = n.Type,
                        title = n.Title,
                        body = n.Body,
                        is_read = n.IsRead,
                        created_at = n.CreatedAt,
                        action_url = n.ActionUrl
                    })
                    .ToListAsync();

                return Ok(new
                {
                    unread_count = unreadCount,
                    notifications = items
                });
            }
            else
            {
                // Instructor
                var query = _context.Notifications.AsNoTracking().Where(n => n.UserId == userId).AsQueryable();

                if (type.HasValue)
                {
                    // Compute the filter value as a constant string - PostgreSQL can translate this correctly
                    var typeStr = type.Value.ToString().ToLower();
                    query = query.Where(n => n.Type == typeStr);
                }
                if (unread_only)
                {
                    query = query.Where(n => !n.IsRead);
                }

                var totalItems = await query.CountAsync();
                var notifications = await query
                    .OrderByDescending(n => n.CreatedAt)
                    .Skip((page - 1) * actualPerPage)
                    .Take(actualPerPage)
                    .Select(n => new
                    {
                        id = n.Id,
                        type = n.Type,
                        title = n.Title,
                        body = n.Body,
                        is_read = n.IsRead,
                        created_at = n.CreatedAt,
                        action_url = n.ActionUrl
                    })
                    .ToListAsync();

                return Ok(new
                {
                    data = notifications,
                    meta = new
                    {
                        page,
                        limit = actualPerPage,
                        total_items = totalItems,
                        total_pages = (int)Math.Ceiling((double)totalItems / actualPerPage)
                    }
                });
            }
        }

        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkRead(string id)
        {
            var userId = GetUserId();
            var isStudent = IsStudent();

            if (isStudent)
            {
                var student = await _context.Students.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == userId);
                if (student == null) return Unauthorized();

                var notification = await _context.Notifications.AsNoTracking().FirstOrDefaultAsync(n => n.Id == id && n.StudentId == student.Id);
                if (notification == null) return NotFound(new { error = "not_found", message = _localizer.GetMessage("NOTIFICATION_NOT_FOUND") });

                await _notificationService.MarkAsReadAsync(id, student.Id, isStudent: true);
                var unreadCount = await _notificationService.GetUnreadCountAsync(student.Id, isStudent: true);

                return Ok(new
                {
                    notification_id = notification.Id,
                    is_read = true,
                    read_at = DateTimeOffset.UtcNow,
                    remaining_unread_count = unreadCount
                });
            }
            else
            {
                var notification = await _context.Notifications.AsNoTracking().FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
                if (notification == null) return NotFound();

                await _notificationService.MarkAsReadAsync(id, userId, isStudent: false);

                notification.IsRead = true;
                notification.ReadAt = DateTimeOffset.UtcNow;

                return Ok(new { data = notification });
            }
        }

        [HttpPost("read-all")]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = GetUserId();
            var isStudent = IsStudent();

            if (isStudent)
            {
                var student = await _context.Students.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == userId);
                if (student == null) return Unauthorized();

                await _notificationService.MarkAllAsReadAsync(student.Id, isStudent: true);

                return Ok(new { message = _localizer.GetMessage("NOTIFICATIONS_MARKED_READ") });
            }
            else
            {
                await _notificationService.MarkAllAsReadAsync(userId, isStudent: false);

                return Ok(new { data = new { message = _localizer.GetMessage("NOTIFICATIONS_MARKED_READ") } });
            }
        }
    }
}
