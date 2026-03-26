using System.Threading.Tasks;

namespace IbnElgm3a.Services
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string toEmail, string subject, string body);
        Task<bool> SendPasswordResetEmailAsync(string toEmail, string otpCode, string userName = "المستخدم");
        Task<bool> SendWelcomeEmailAsync(string toEmail, string name, string password);
    }
}
