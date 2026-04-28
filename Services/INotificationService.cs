using System.Threading.Tasks;

namespace IbnElgm3a.Services
{
    public interface INotificationService
    {
        Task<int> GetUnreadCountAsync(string studentId);
        Task MarkAsReadAsync(string notificationId, string studentId);
        Task MarkAllAsReadAsync(string studentId);
        Task InvalidateCacheAsync(string studentId);
        Task CreateNotificationAsync(string studentId, string type, string title, string body, string? actionUrl = null);
    }
}
