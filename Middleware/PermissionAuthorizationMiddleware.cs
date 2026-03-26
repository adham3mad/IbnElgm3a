using IbnElgm3a.Model;
using IbnElgm3a.Models;
using IbnElgm3a.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using IbnElgm3a.Services.Localization;
using IbnElgm3a.Filters;

namespace IbnElgm3a.Middleware
{
    public class PermissionAuthorizationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILocalizationService _localizer;

        public PermissionAuthorizationMiddleware(RequestDelegate next, ILocalizationService localizer)
        {
            _next = next;
            _localizer = localizer;
        }

        public async Task InvokeAsync(HttpContext context, AppDbContext dbContext)
        {
            var endpoint = context.GetEndpoint();
            if (endpoint == null)
            {
                await _next(context);
                return;
            }

            var allowAnonymous = endpoint.Metadata.GetMetadata<Microsoft.AspNetCore.Authorization.IAllowAnonymous>();
            if (allowAnonymous != null)
            {
                await _next(context);
                return;
            }

            var requirePermissionAttribute = endpoint.Metadata.GetMetadata<RequirePermissionAttribute>();
            if (requirePermissionAttribute == null)
            {
                await _next(context);
                return;
            }

            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                await ReturnForbidden(context, "AUTH_REQUIRED", _localizer.GetMessage("UNAUTHORIZED"));
                return;
            }

            // Check user permission in DB
            var permissionCode = requirePermissionAttribute.PermissionCode;

            var hasPermission = await dbContext.Users
                .Where(u => u.Id == userId && u.IsActive)
                .AnyAsync(u => u.Role != null && u.Role.Permissions.Any(p => p.Code == permissionCode));

            if (!hasPermission)
            {
                await ReturnForbidden(context, "FORBIDDEN", _localizer.GetMessage("FORBIDDEN"));
                return;
            }

            await _next(context);
        }

        private static async Task ReturnForbidden(HttpContext context, string errorCode, string message)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            context.Response.ContentType = "application/json";

            var errorResponse = ApiResponse<object>.CreateError(errorCode, message);
            var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            });

            await context.Response.WriteAsync(json);
        }
    }
}
