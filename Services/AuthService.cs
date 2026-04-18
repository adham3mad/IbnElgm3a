using IbnElgm3a.DTOs.Auth;
using IbnElgm3a.Enums;
using IbnElgm3a.Models;
using IbnElgm3a.Models.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace IbnElgm3a.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _config;

        public AuthService(AppDbContext context, IConfiguration config, IEmailService emailService)
        {
            _context = context;
            _config = config;
            _emailService = emailService;
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request)
        {
            var pepper = _config["PASSWORD_PEPPER"] ?? "";
            var user = await _context.Users
            .Include(u => u.Role)
                .ThenInclude(r => r!.Permissions)
                    .ThenInclude(p => p.Feature)
            .FirstOrDefaultAsync(u => u.NationalId == request.NationalId);
            
            bool isPasswordCorrect = false;
            if (user != null)
            {
                try
                {
                    isPasswordCorrect = BCrypt.Net.BCrypt.Verify(request.Password + pepper, user.PasswordHash);
                }
                catch { } 
                
                if (!isPasswordCorrect && request.Password == user.PasswordHash) 
                    isPasswordCorrect = true;
            }

            if (user == null || !isPasswordCorrect)
            {
                return null;
            }

            return new LoginResponseDto
            {
                Tokens = await GenerateTokensAsync(user, request.RememberMe ?? false),
                User = await MapToAuthUserDtoAsync(user)
            };
        }

        public async Task<AuthTokensDto?> RefreshTokenAsync(RefreshRequestDto request)
        {
            var tokenEntity = await _context.Tokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TokenValue == request.RefreshToken && t.TokenType == "refresh");

            if (tokenEntity == null || tokenEntity.IsRevoked || tokenEntity.ExpiryDate < DateTimeOffset.UtcNow)
                return null;

            return await GenerateAccessTokensAsync(tokenEntity.User!, false, request.RefreshToken);
        }

        public async Task<bool> LogoutAsync(LogoutRequestDto request, string userId)
        {
            if (request.AllDevices == true)
            {
                var tokens = await _context.Tokens.Where(t => t.UserId == userId).ToListAsync();
                foreach (var t in tokens) t.IsRevoked = true;
            }
            else
            {
                var token = await _context.Tokens.FirstOrDefaultAsync(t => t.TokenValue == request.RefreshToken);
                if (token != null) token.IsRevoked = true;
            }
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<(string Channel, int ExpiresIn)?> RequestForgotPasswordAsync(ForgotPasswordRequestDto request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.NationalId == request.NationalId);
            if (user == null) return null;

            // Generate a 6-digit OTP
            var otpCode = new Random().Next(100000, 999999).ToString();
            
            // Invert previous OTPs for this user to prevent spam
            var oldOtps = await _context.Tokens
                .Where(t => t.UserId == user.Id && t.TokenType == "otp" && !t.IsRevoked)
                .ToListAsync();
            foreach (var old in oldOtps) old.IsRevoked = true;

            var tokenEntity = new Token
            {
                UserId = user.Id,
                TokenValue = otpCode,
                TokenType = "otp",
                ExpiryDate = DateTimeOffset.UtcNow.AddMinutes(15)
            };
            
            _context.Tokens.Add(tokenEntity);
            await _context.SaveChangesAsync();

            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendPasswordResetEmailAsync(user.Email, otpCode, user.Name);
                }
                catch (Exception) { }
            });

            return (request.Channel, 900);
        }

        public async Task<string?> VerifyOtpAsync(VerifyOtpRequestDto request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.NationalId == request.NationalId);
            if (user == null) return null;

            var activeOtp = await _context.Tokens
                .FirstOrDefaultAsync(t => t.UserId == user.Id && 
                                          t.TokenValue == request.OtpCode && 
                                          t.TokenType == "otp" &&
                                          !t.IsRevoked && 
                                          t.ExpiryDate > DateTimeOffset.UtcNow);

            if (activeOtp == null) return null;

            activeOtp.IsRevoked = true;

            // Generate a secure Reset Token (Valid for 15 mins)
            var resetTokenStr = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
            var resetToken = new Token
            {
                UserId = user.Id,
                TokenValue = resetTokenStr,
                TokenType = "reset_password",
                ExpiryDate = DateTimeOffset.UtcNow.AddMinutes(15)
            };

            _context.Tokens.Add(resetToken);
            await _context.SaveChangesAsync();

            return resetTokenStr;
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordRequestDto request)
        {
            var resetToken = await _context.Tokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TokenValue == request.ResetToken && 
                                          t.TokenType == "reset_password" &&
                                          !t.IsRevoked && 
                                          t.ExpiryDate > DateTimeOffset.UtcNow);

            if (resetToken == null || resetToken.User == null) return false;

            // Revoke the reset token so it cannot be reused
            resetToken.IsRevoked = true;

            // Hash new password
            var pepper = _config["PASSWORD_PEPPER"] ?? "";
            resetToken.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword + pepper);
            
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<LoginResponseDto?> BiometricLoginAsync(BiometricLoginRequestDto request)
        {
            var device = await _context.Devices
                .Include(d => d.User)
                    .ThenInclude(u => u!.Role)
                        .ThenInclude(r => r!.Permissions)
                            .ThenInclude(p => p.Feature)
                .FirstOrDefaultAsync(d => d.DeviceId == request.DeviceId && 
                                          d.BiometricPublicKey == request.BiometricSignature && 
                                          d.IsActive);

            if (device == null || device.User == null || device.User.Status != IbnElgm3a.Enums.UserStatus.Active) 
                return null;

            device.LastUsedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();

            return new LoginResponseDto
            {
                Tokens = await GenerateTokensAsync(device.User, true),
                User = await MapToAuthUserDtoAsync(device.User)
            };
        }

        public async Task<string> GetBiometricChallengeAsync()
        {
            // Simple challenge for now, could be stored in cache/session if needed
            return await Task.FromResult(Guid.NewGuid().ToString("N"));
        }

        private async Task<AuthUserDto> MapToAuthUserDtoAsync(User user)
        {
            var roleName = user.Role?.Name?.ToLower() ?? "student";
            var roleEnum = Enum.TryParse<UserRole>(roleName, true, out var r) ? r : UserRole.Student;
            
            var dto = new AuthUserDto
            {
                Id = user.Id,
                Role = roleEnum,
                FullName = user.Name,
                Email = user.Email,
                AvatarUrl = user.AvatarUrl,
                FacultyId = user.FacultyId,
                MustChangePw = user.MustChangePw
            };

            // Check if user is a sub-admin to get scope
            var subAdmin = await _context.SubAdmins.FirstOrDefaultAsync(s => s.UserId == user.Id && s.IsActive);
            if (subAdmin != null)
            {
                dto.ScopeType = subAdmin.ScopeType;
                dto.ScopeId = subAdmin.ScopeId;
            }

            // Permissions based on Role
            var permissions = user.Role?.Permissions.ToList() ?? new List<Permission>();
            
            if (roleEnum == UserRole.Admin)
            {
                // Super Admin gets all permissions
                permissions = await _context.Permissions.Include(p => p.Feature).ToListAsync();
            }

            dto.Permissions = permissions
                .Where(p => p.Feature != null)
                .GroupBy(p => p.FeatureId)
                .Select(g => new FeatureDto
                {
                    Name = g.First().Feature.Name,
                    NameAr = g.First().Feature.NameAr,
                    Permissions = g.Select(p => new PermissionDto
                    {
                        Name = p.Name,
                        NameAr = p.Ar_Name
                    }).ToList()
                }).ToList();

            return dto;
        }

        public async Task<bool> RegisterBiometricAsync(BiometricRegisterRequestDto request, string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            // Optional: Invalidate old device if overriding
            var existingDevice = await _context.Devices.FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceId == request.DeviceId);
            if (existingDevice != null)
            {
                existingDevice.BiometricPublicKey = request.PublicKey;
                existingDevice.DeviceName = request.DeviceName;
                existingDevice.LastUsedAt = DateTimeOffset.UtcNow;
                existingDevice.IsActive = true;
            }
            else
            {
                _context.Devices.Add(new UserDevice
                {
                    Id = "dev_" + Guid.NewGuid().ToString("N").Substring(0, 10),
                    UserId = userId,
                    DeviceId = request.DeviceId,
                    DeviceName = request.DeviceName,
                    BiometricPublicKey = request.PublicKey,
                    IsActive = true
                });
            }

            await _context.SaveChangesAsync();
            return true;
        }

        private async Task<AuthTokensDto> GenerateTokensAsync(User user, bool rememberMe)
        {
            var jwtKey = _config["JWT_KEY"] ?? "MasaarVerySecureSuperSecretKey123456!!";
            var issuer = _config["JWT_ISSUER"] ?? "Masaar";
            var audience = _config["JWT_AUDIENCE"] ?? "MasaarClient";

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Fetch sub-admin scope if applicable
            var subAdmin = await _context.SubAdmins.FirstOrDefaultAsync(s => s.UserId == user.Id && s.IsActive);
            
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("role", user.Role?.Name?.ToLower() ?? "student"),
                new Claim("faculty_id", user.FacultyId ?? ""),
                new Claim("department_id", user.DepartmentId ?? ""),
                new Claim("scope_type", subAdmin?.ScopeType.ToString().ToLower() ?? "null"),
                new Claim("scope_id", subAdmin?.ScopeId ?? "null")
            };

            var expiryMinutes = 15;
            var expiry = DateTime.UtcNow.AddMinutes(expiryMinutes);
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiry,
                signingCredentials: creds
            );

            var refreshTokenTtlDays = rememberMe ? 90 : 30;

            var tokens = new AuthTokensDto
            {
                AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
                RefreshToken = Guid.NewGuid().ToString("N"),
                ExpiresIn = expiryMinutes * 60,
                ExpiresAt = expiry
            };

            // Save Refresh Token to DB
            _context.Tokens.Add(new Token
            {
                UserId = user.Id,
                TokenValue = tokens.RefreshToken,
                TokenType = "refresh",
                ExpiryDate = DateTimeOffset.UtcNow.AddDays(refreshTokenTtlDays)
            });

            // Save Access Token to DB for consistency
            _context.Tokens.Add(new Token
            {
                UserId = user.Id,
                TokenValue = tokens.AccessToken,
                TokenType = "access",
                ExpiryDate = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes)
            });
            
            await _context.SaveChangesAsync();

            return tokens;
        }
    
        private async Task<AuthTokensDto> GenerateAccessTokensAsync(User user, bool rememberMe, string RefreshToken)
        {
            var jwtKey = _config["JWT_KEY"] ?? "MasaarVerySecureSuperSecretKey123456!!";
            var issuer = _config["JWT_ISSUER"] ?? "Masaar";
            var audience = _config["JWT_AUDIENCE"] ?? "MasaarClient";

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var subAdmin = await _context.SubAdmins.FirstOrDefaultAsync(s => s.UserId == user.Id && s.IsActive);
            
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("role", user.Role?.Name?.ToLower() ?? "student"),
                new Claim("faculty_id", user.FacultyId ?? ""),
                new Claim("department_id", user.DepartmentId ?? ""),
                new Claim("scope_type", subAdmin?.ScopeType.ToString().ToLower() ?? "null"),
                new Claim("scope_id", subAdmin?.ScopeId ?? "null")
            };

            var expiryMinutes = 15;
            var expiry = DateTime.UtcNow.AddMinutes(expiryMinutes);
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiry,
                signingCredentials: creds
            );

            var tokens = new AuthTokensDto
            {
                AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
                RefreshToken = RefreshToken,
                ExpiresIn = expiryMinutes * 60,
                ExpiresAt = expiry
            };

            _context.Tokens.Add(new Token
            {
                UserId = user.Id,
                TokenValue = tokens.AccessToken,
                TokenType = "access",
                ExpiryDate = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes)
            });
            await _context.SaveChangesAsync();

            return tokens;
        }
    }
}
