namespace IbnElgm3a.Services
{
    public static class EmailTemplates
    {
        private const string PrimaryColor = "#2e7d32"; // Dark Green
        private const string BackgroundColor = "#f4f4f4";
        private const string CardBackground = "#ffffff";
        private const string TextColor = "#333333";
        private const string SubtitleColor = "#666666";

        private static string GetBaseLayout(string title, string content)
        {
            return $@"
<!DOCTYPE html>
<html dir='rtl'>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: {BackgroundColor}; margin: 0; padding: 0; color: {TextColor}; }}
        .container {{ max-width: 600px; margin: 20px auto; background-color: {CardBackground}; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1); }}
        .header {{ background-color: {PrimaryColor}; padding: 30px; text-align: center; color: white; }}
        .header h1 {{ margin: 0; font-size: 24px; }}
        .content {{ padding: 40px; line-height: 1.6; text-align: right; }}
        .footer {{ background-color: #eeeeee; padding: 20px; text-align: center; font-size: 12px; color: {SubtitleColor}; }}
        .button {{ display: inline-block; padding: 12px 25px; background-color: {PrimaryColor}; color: white; text-decoration: none; border-radius: 5px; font-weight: bold; margin-top: 20px; }}
        .otp-box {{ background-color: #f0fdf4; border: 2px dashed {PrimaryColor}; padding: 20px; text-align: center; font-size: 32px; font-weight: bold; letter-spacing: 5px; color: {PrimaryColor}; margin: 20px 0; border-radius: 8px; }}
        .info-row {{ margin-bottom: 15px; border-bottom: 1px solid #eee; padding-bottom: 10px; }}
        .info-label {{ font-weight: bold; color: {PrimaryColor}; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>{title}</h1>
        </div>
        <div class='content'>
            {content}
        </div>
        <div class='footer'>
            &copy; {System.DateTime.Now.Year} IbnElgm3a (ابن الجمعة) - جميع الحقوق محفوظة
        </div>
    </div>
</body>
</html>";
        }

        public static string GetPasswordResetTemplate(string name, string otp)
        {
            var content = $@"
                <p>مرحباً {name}،</p>
                <p>لقد تلقينا طلباً لإعادة تعيين كلمة المرور الخاصة بحسابك. يرجى استخدام رمز التحقق أدناه لإكمال العملية:</p>
                <div class='otp-box'>{otp}</div>
                <p>هذا الرمز صالح لمدة 15 دقيقة فقط.</p>
                <p>إذا لم تكن أنت من قام بهذا الطلب، فيرجى تجاهل هذا البريد الإلكتروني وتأمين حسابك.</p>
            ";
            return GetBaseLayout("إعادة تعيين كلمة المرور", content);
        }

        public static string GetWelcomeEmailTemplate(string name, string email, string password)
        {
            var content = $@"
                <p>مرحباً {name}،</p>
                <p>يسعدنا انضمامك إلينا في نظام ابن الجمعة (IbnElgm3a). تم إنشاء حسابك بنجاح، إليك بيانات الدخول الخاصة بك:</p>
                <div style='background: #f9f9f9; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                    <div class='info-row'>
                        <span class='info-label'>البريد الإلكتروني:</span> {email}
                    </div>
                    <div class='info-row'>
                        <span class='info-label'>كلمة المرور المؤقتة:</span> {password}
                    </div>
                </div>
                <p style='color: #d32f2f; font-weight: bold;'>تنبيه: يرجى تغيير كلمة المرور بمجرد تسجيل الدخول لأول مرة لضمان أمان حسابك.</p>
                <div style='text-align: center;'>
                    <a href='https://ibnelgm3a.com/login' class='button'>تسجيل الدخول الآن</a>
                </div>
            ";
            return GetBaseLayout("مرحباً بك في ابن الجمعة", content);
        }

        public static string GetGeneralNotificationTemplate(string title, string message, string? actionUrl = null, string? actionText = null)
        {
            var actionHtml = !string.IsNullOrEmpty(actionUrl) ? $@"<div style='text-align: center;'><a href='{actionUrl}' class='button'>{actionText ?? "عرض التفاصيل"}</a></div>" : "";
            var content = $@"
                <h2>{title}</h2>
                <p>{message}</p>
                {actionHtml}
            ";
            return GetBaseLayout("تنبيه من النظام", content);
        }
    }
}
