using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using IbnElgm3a.Model;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace IbnElgm3a.Services
{
    public class TokenCleanupService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<TokenCleanupService> _logger;

        public TokenCleanupService(IServiceProvider services, ILogger<TokenCleanupService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Token Cleanup Background Service is starting.");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _services.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    
                    var expiredTokens = await dbContext.Tokens
                        .Where(t => t.ExpiryDate <= DateTimeOffset.UtcNow || t.IsRevoked)
                        .ToListAsync(stoppingToken);

                    if (expiredTokens.Any())
                    {
                        dbContext.Tokens.RemoveRange(expiredTokens);
                        await dbContext.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation($"Cleaned up {expiredTokens.Count} expired/revoked tokens from the database.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing token cleanup.");
                }

                // Run once every hour
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }
}
