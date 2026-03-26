using IbnElgm3a.Models;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using IbnElgm3a.Services.Localization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace IbnElgm3a.Middleware
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly ILocalizationService _localizer;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IWebHostEnvironment env, ILocalizationService localizer)
        {
            _logger = logger;
            _env = env;
            _localizer = localizer;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);

            var statusCode = StatusCodes.Status500InternalServerError;
            var errorCode = "INTERNAL_SERVER_ERROR";
            var message = _localizer.GetMessage("UNEXPECTED_ERROR") ?? "An unexpected error occurred.";
            var messageAr = "حدث خطأ غير متوقع.";

            // Handle specific exception types
            if (exception is UnauthorizedAccessException)
            {
                statusCode = StatusCodes.Status401Unauthorized;
                errorCode = "UNAUTHORIZED";
                message = _localizer.GetMessage("UNAUTHORIZED_ACCESS") ?? "Unauthorized access.";
                messageAr = "وصول غير مصرح به.";
            }
            else if (exception is KeyNotFoundException)
            {
                statusCode = StatusCodes.Status404NotFound;
                errorCode = "NOT_FOUND";
                message = _localizer.GetMessage("RESOURCE_NOT_FOUND") ?? "Resource not found.";
                messageAr = "المورد غير موجود.";
            }
            else if (exception is Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                // Generic DB error, could be handled more specifically if needed
                errorCode = "DATABASE_ERROR";
                message = "A database error occurred.";
                messageAr = "حدث خطأ في قاعدة البيانات.";
            }

            var response = ApiResponse<object>.CreateError(
                code: errorCode,
                message: _env.IsDevelopment() ? $"{message} | {exception.Message}" : message,
                messageAr: messageAr
            );

            if (httpContext.Request.Headers.TryGetValue("X-Request-ID", out var reqId))
            {
                response.Meta.RequestId = reqId.ToString();
            }
            
            // Add stack trace in development
            if (_env.IsDevelopment())
            {
                response.Data = new { detail = exception.StackTrace };
            }

            httpContext.Response.StatusCode = statusCode;
            httpContext.Response.ContentType = "application/json";
            
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            await httpContext.Response.WriteAsJsonAsync(response, options, cancellationToken);

            return true;
        }
    }
}
