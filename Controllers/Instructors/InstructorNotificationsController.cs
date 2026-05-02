using IbnElgm3a.Models;
using IbnElgm3a.Models.Data;
using IbnElgm3a.Services;
using IbnElgm3a.Services.Localization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IbnElgm3a.Controllers.Instructors
{
    [ApiController]
    [Route("instructor/notifications")]
    [Authorize(Roles = "instructor")]
    public class InstructorNotificationsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILocalizationService _localizer;

        public InstructorNotificationsController(AppDbContext context, ILocalizationService localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        [HttpGet]
        public async Task<IActionResult> GetNotifications([FromQuery] int page = 1, [FromQuery] int limit = 20)
        {
            var userId = GetUserId();
            var query = _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt);

            var totalItems = await query.CountAsync();
            var notifications = await query
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            return Ok(new
            {
                data = notifications,
                meta = new
                {
                    page,
                    limit,
                    total_items = totalItems,
                    total_pages = (int)Math.Ceiling((double)totalItems / limit)
                }
            });
        }

        [HttpPatch("read-all")]
        public async Task<IActionResult> ReadAll()
        {
            var userId = GetUserId();
            var unread = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var n in unread)
            {
                n.IsRead = true;
                n.ReadAt = DateTimeOffset.UtcNow;
            }

            await _context.SaveChangesAsync();
            return Ok(new { data = new { message = _localizer.GetMessage("NOTIFICATIONS_MARKED_READ") } });
        }

        [HttpPatch("{id}/read")]
        public async Task<IActionResult> ReadOne(string id)
        {
            var userId = GetUserId();
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (notification == null) return NotFound();

            notification.IsRead = true;
            notification.ReadAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { data = notification });
        }
    }
}
