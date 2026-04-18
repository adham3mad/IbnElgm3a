using IbnElgm3a.Models;
using IbnElgm3a.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace IbnElgm3a.Middleware
{
    public class TokenValidationMiddleware
    {
        private readonly RequestDelegate _next;

        public TokenValidationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, AppDbContext dbContext)
        {
            // Skip validation if the endpoint specifically allows anonymous access
            var endpoint = context.GetEndpoint();
            var allowAnonymous = endpoint?.Metadata?.GetMetadata<Microsoft.AspNetCore.Authorization.IAllowAnonymous>();
            if (allowAnonymous != null)
            {
                await _next(context);
                return;
            }

            if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                var tokenValues = authHeader.FirstOrDefault()?.Split(" ");
                if (tokenValues != null && tokenValues.Length == 2 && tokenValues[0].Equals("Bearer", System.StringComparison.OrdinalIgnoreCase))
                {
                    var actualToken = tokenValues[1];

                    // Check if the token exists, is not revoked, and is not expired
                    var isTokenValid = await dbContext.Tokens
                        .AnyAsync(t => t.TokenValue == actualToken &&
                                       !t.IsRevoked &&
                                       t.ExpiryDate > System.DateTimeOffset.UtcNow);

                    if (!isTokenValid)
                    {
                        await ReturnUnauthorized(context, "INVALID_OR_EXPIRED_TOKEN", "Your session is invalid, expired, or has been revoked.");
                        return;
                    }
                }
            }

            await _next(context);
        }

        private static async Task ReturnUnauthorized(HttpContext context, string errorCode, string message)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
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
