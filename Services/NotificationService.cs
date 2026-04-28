using IbnElgm3a.Models;
using IbnElgm3a.Models.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IbnElgm3a.Services
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;
        private readonly IDistributedCache _cache;

        public NotificationService(AppDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
        }

        private string GetCacheKey(string studentId) => $"student_unread_notifications_{studentId}";

        public async Task<int> GetUnreadCountAsync(string studentId)
        {
            var cacheKey = GetCacheKey(studentId);
            var cachedValue = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedValue) && int.TryParse(cachedValue, out int count))
            {
                return count;
            }

            int unreadCount = await _context.Notifications
                .CountAsync(n => n.StudentId == studentId && !n.IsRead);

            // Cache for 30 minutes
            await _cache.SetStringAsync(cacheKey, unreadCount.ToString(), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
            });

            return unreadCount;
        }

        public async Task InvalidateCacheAsync(string studentId)
        {
            await _cache.RemoveAsync(GetCacheKey(studentId));
        }

        public async Task MarkAsReadAsync(string notificationId, string studentId)
        {
            var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId && n.StudentId == studentId);
            if (notification != null && !notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTimeOffset.UtcNow;
                await _context.SaveChangesAsync();
                await InvalidateCacheAsync(studentId);
            }
        }

        public async Task MarkAllAsReadAsync(string studentId)
        {
            var unread = await _context.Notifications
                .Where(n => n.StudentId == studentId && !n.IsRead)
                .ToListAsync();

            if (unread.Any())
            {
                var now = DateTimeOffset.UtcNow;
                foreach (var n in unread)
                {
                    n.IsRead = true;
                    n.ReadAt = now;
                }
                await _context.SaveChangesAsync();
                await InvalidateCacheAsync(studentId);
            }
        }

        public async Task CreateNotificationAsync(string studentId, string type, string title, string body, string? actionUrl = null)
        {
            var notification = new Notification
            {
                StudentId = studentId,
                Type = type,
                Title = title,
                Body = body,
                ActionUrl = actionUrl,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            await InvalidateCacheAsync(studentId);
        }
    }
}
