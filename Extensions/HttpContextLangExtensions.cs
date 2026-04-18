namespace IbnElgm3a.Extensions
{
    public static class HttpContextLangExtensions
    {
        public static string Lang(this HttpContext context)
            => context.Items["Lang"]?.ToString() ?? "En";
    }

}
