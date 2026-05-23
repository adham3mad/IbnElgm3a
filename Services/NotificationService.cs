using IbnElgm3a.Models;
using IbnElgm3a.Models.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IbnElgm3a.Services
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(AppDbContext context, IDistributedCache cache, ILogger<NotificationService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        private string GetCacheKey(string targetId, bool isStudent) => 
            isStudent ? $"student_unread_notifications_{targetId}" : $"instructor_unread_notifications_{targetId}";

        public async Task<int> GetUnreadCountAsync(string targetId, bool isStudent = true)
        {
            var cacheKey = GetCacheKey(targetId, isStudent);
            try
            {
                var cachedValue = await _cache.GetStringAsync(cacheKey);

                if (!string.IsNullOrEmpty(cachedValue) && int.TryParse(cachedValue, out int count))
                {
                    return count;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read from cache for key {CacheKey}. Falling back to database.", cacheKey);
            }

            int unreadCount;
            if (isStudent)
            {
                unreadCount = await _context.Notifications
                    .CountAsync(n => n.StudentId == targetId && !n.IsRead);
            }
            else
            {
                unreadCount = await _context.Notifications
                    .CountAsync(n => n.UserId == targetId && !n.IsRead);
            }

            try
            {
                // Cache for 30 minutes
                await _cache.SetStringAsync(cacheKey, unreadCount.ToString(), new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to write to cache for key {CacheKey}.", cacheKey);
            }

            return unreadCount;
        }

        public async Task InvalidateCacheAsync(string targetId, bool isStudent = true)
        {
            var cacheKey = GetCacheKey(targetId, isStudent);
            try
            {
                await _cache.RemoveAsync(cacheKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to invalidate cache for key {CacheKey}.", cacheKey);
            }
        }

        public async Task MarkAsReadAsync(string notificationId, string targetId, bool isStudent = true)
        {
            int updatedRows = 0;
            var now = DateTimeOffset.UtcNow;
            if (isStudent)
            {
                updatedRows = await _context.Notifications
                    .Where(n => n.Id == notificationId && n.StudentId == targetId && !n.IsRead)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(n => n.IsRead, true)
                        .SetProperty(n => n.ReadAt, now));
            }
            else
            {
                updatedRows = await _context.Notifications
                    .Where(n => n.Id == notificationId && n.UserId == targetId && !n.IsRead)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(n => n.IsRead, true)
                        .SetProperty(n => n.ReadAt, now));
            }

            if (updatedRows > 0)
            {
                await InvalidateCacheAsync(targetId, isStudent);
            }
        }

        public async Task MarkAllAsReadAsync(string targetId, bool isStudent = true)
        {
            int updatedRows = 0;
            var now = DateTimeOffset.UtcNow;
            if (isStudent)
            {
                updatedRows = await _context.Notifications
                    .Where(n => n.StudentId == targetId && !n.IsRead)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(n => n.IsRead, true)
                        .SetProperty(n => n.ReadAt, now));
            }
            else
            {
                updatedRows = await _context.Notifications
                    .Where(n => n.UserId == targetId && !n.IsRead)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(n => n.IsRead, true)
                        .SetProperty(n => n.ReadAt, now));
            }

            if (updatedRows > 0)
            {
                await InvalidateCacheAsync(targetId, isStudent);
            }
        }

        public async Task CreateNotificationAsync(string targetId, string type, string title, string body, string? actionUrl = null, bool isStudent = true)
        {
            var notification = new Notification
            {
                Type = type,
                Title = title,
                Body = body,
                ActionUrl = actionUrl,
                IsRead = false
            };

            if (isStudent)
            {
                notification.StudentId = targetId;
            }
            else
            {
                notification.UserId = targetId;
            }

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            await InvalidateCacheAsync(targetId, isStudent);
        }
    }
}
