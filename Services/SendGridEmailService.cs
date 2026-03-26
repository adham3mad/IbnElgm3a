using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Threading.Tasks;

namespace IbnElgm3a.Services
{
    public class SendGridEmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<SendGridEmailService> _logger;

        public SendGridEmailService(IOptions<EmailSettings> settings, ILogger<SendGridEmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var client = new SendGridClient(_settings.ApiKey);
                var from = new EmailAddress(_settings.SenderEmail, _settings.SenderName);
                var to = new EmailAddress(toEmail);
                var msg = MailHelper.CreateSingleEmail(from, to, subject, body, body);
                var response = await client.SendEmailAsync(msg);

                if (!response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Body.ReadAsStringAsync();
                    _logger.LogError("SendGrid Email Sending Failed. Status Code: {StatusCode}, Body: {Body}", response.StatusCode, responseBody);
                }
                else
                {
                    _logger.LogInformation("Email sent successfully to {ToEmail}", toEmail);
                }

                return response.IsSuccessStatusCode;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while sending email to {ToEmail}", toEmail);
                return false;
            }
        }

        public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string otpCode, string userName = "المستخدم")
        {
            var subject = "إعادة تعيين كلمة المرور - Massar";
            var body = EmailTemplates.GetPasswordResetTemplate(userName, otpCode);
            
            return await SendEmailAsync(toEmail, subject, body);
        }

        public async Task<bool> SendWelcomeEmailAsync(string toEmail, string name, string password)
        {
            var subject = "مرحباً بك في ابن الجمعة - بيانات حسابك";
            var body = EmailTemplates.GetWelcomeEmailTemplate(name, toEmail, password);
            
            return await SendEmailAsync(toEmail, subject, body);
        }
    }
}
