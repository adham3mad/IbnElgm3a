using System.Threading.Tasks;

namespace IbnElgm3a.Services
{
    public interface INotificationService
    {
        Task<int> GetUnreadCountAsync(string targetId, bool isStudent = true);
        Task MarkAsReadAsync(string notificationId, string targetId, bool isStudent = true);
        Task MarkAllAsReadAsync(string targetId, bool isStudent = true);
        Task InvalidateCacheAsync(string targetId, bool isStudent = true);
        Task CreateNotificationAsync(string targetId, string type, string title, string body, string? actionUrl = null, bool isStudent = true);
    }
}
