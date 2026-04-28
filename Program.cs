using DotNetEnv;
using IbnElgm3a.Middleware;
using IbnElgm3a.Models;
using IbnElgm3a.Models.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Globalization;
using System.Text;
using Serilog;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace IbnElgm3a
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
            if (File.Exists(envPath))
            {
                Env.Load();
            }

            var builder = WebApplication.CreateBuilder(args);

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            builder.Host.UseSerilog();
            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog();

            // Add services to the container.

            builder.Services.AddControllers(options =>
            {
                options.Filters.Add<IbnElgm3a.Filters.StandardResponseWrapperFilter>();
            });

            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
            builder.Services.AddProblemDetails();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var connString = Environment.GetEnvironmentVariable("CS");
            try
            {
                using var conn = new Npgsql.NpgsqlConnection(connString);
                conn.Open();
                Console.WriteLine("Database Connected Successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database connection failed: {ex.Message}");
                throw;
            }

            // Encryption Service
            builder.Services.AddSingleton<IbnElgm3a.Services.IAesEncryptionService>(sp =>
            {
                var key = Environment.GetEnvironmentVariable("DB_ENCRYPTION_KEY") ?? "";
                return new IbnElgm3a.Services.AesEncryptionService(key);
            });

            // Register DbContext
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connString));

            builder.Services.Configure<EmailSettings>(options =>
            {
                options.ApiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY")
                    ?? throw new Exception("SendGrid API Key is missing");

                options.SenderEmail = Environment.GetEnvironmentVariable("SENDGRID_SENDER_EMAIL")
                    ?? throw new Exception("SendGrid Sender Email is missing");

                options.SenderName =
                    Environment.GetEnvironmentVariable("SENDGRID_SENDER_NAME") ?? "IbnElgm3a";
            });

            var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY");
            var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER");
            var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE");

            if (string.IsNullOrEmpty(jwtKey))
                throw new Exception("JWT_KEY is not set or empty!");

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    var keyBytes = Encoding.UTF8.GetBytes(jwtKey);
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtIssuer,
                        ValidAudience = jwtAudience,
                        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                        ClockSkew = TimeSpan.Zero
                    };
                });

            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "IbnElgm3a API",
                    Version = "v1"
                });

                // JWT Auth
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            // Redis Cache
            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = Environment.GetEnvironmentVariable("REDIS_CS") ?? "localhost:6379";
                options.InstanceName = "IbnElgm3a_";
            });

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll",
                    policy => policy
                        .AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod());
            });

            // Rate Limiting (Production Protection)
            builder.Services.AddRateLimiter(options =>
            {
                options.AddFixedWindowLimiter("auth", opt =>
                {
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.PermitLimit = 5; // Max 5 login attempts per minute per IP
                    opt.QueueLimit = 0;
                });
            });

            // Health Checks
            builder.Services.AddHealthChecks()
                .AddNpgSql(connString ?? "", name: "Database");

            // Global Authorization Policy (Secure by Default)
            builder.Services.AddAuthorization(options =>
            {
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            });

            builder.Services.AddHostedService<IbnElgm3a.Services.TokenCleanupService>();
            // builder.Services.AddScoped<GenerateToken>();
            // builder.Services.AddScoped<Email>();
            // builder.Services.AddScoped<GenerateIdentificationNumber>();
            builder.Services.AddScoped<IbnElgm3a.Services.IAuthService, IbnElgm3a.Services.AuthService>();

            // Localization
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddSingleton<IbnElgm3a.Services.Localization.ILocalizationService, IbnElgm3a.Services.Localization.LocalizationService>();
            builder.Services.AddScoped<IbnElgm3a.Services.IFileStorageService, IbnElgm3a.Services.LocalFileStorageService>();
            builder.Services.AddScoped<IbnElgm3a.Services.IEmailService, IbnElgm3a.Services.SendGridEmailService>();
            builder.Services.AddScoped<IbnElgm3a.Services.INotificationService, IbnElgm3a.Services.NotificationService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            app.UseExceptionHandler();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors("AllowAll");
            app.UseStaticFiles();
            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseMiddleware<IbnElgm3a.Middleware.SecurityHeadersMiddleware>();
            app.UseMiddleware<IbnElgm3a.Middleware.TokenValidationMiddleware>();
            app.UseMiddleware<IbnElgm3a.Middleware.PermissionAuthorizationMiddleware>();
            app.UseAuthorization();
            app.UseRateLimiter();

            app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
            {
                ResultStatusCodes =
                {
                    [HealthStatus.Healthy] = StatusCodes.Status200OK,
                    [HealthStatus.Degraded] = StatusCodes.Status200OK,
                    [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
                }
            });

            var cultures = new[] { new CultureInfo("en"), new CultureInfo("ar") };
            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture("en"),
                SupportedCultures = cultures,
                SupportedUICultures = cultures
            });

            app.MapControllers();

            /* 
                        // Seed Permissions (Moved to Endpoint/Manual trigger)
                        using (var scope = app.Services.CreateScope())
                        {
                            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                            try
                            {
                                await PermissionSeeder.SeedAsync(context);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Seeding failed: {ex.Message}");
                            }
                        }
            */
            app.Run();
        }
    }
}
