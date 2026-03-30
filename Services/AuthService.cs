using IbnElgm3a.DTOs.Auth;
using IbnElgm3a.Enums;
using IbnElgm3a.Model;
using IbnElgm3a.Model.Data;
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
                Tokens = GenerateTokens(user, request.RememberMe ?? false),
                User = MapToAuthUserDto(user)
            };
        }

        public async Task<AuthTokensDto?> RefreshTokenAsync(RefreshRequestDto request)
        {
            var tokenEntity = await _context.Tokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TokenValue == request.RefreshToken && t.TokenType == "refresh");

            if (tokenEntity == null || tokenEntity.IsRevoked || tokenEntity.ExpiryDate < DateTimeOffset.UtcNow)
                return null;

            return GenerateAccessTokens(tokenEntity.User!, false, request.RefreshToken);
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
                catch (Exception)
                {
                    // Log error if needed, but since we are fire-and-forget, we just catch to prevent crash
                }
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

            // Mark OTP as used
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
                Tokens = GenerateTokens(device.User, true),
                User = MapToAuthUserDto(device.User)
            };
        }

        private AuthUserDto MapToAuthUserDto(User user)
        {
            var roleEnum = Enum.TryParse<UserRole>(user.Role?.Name ?? "", true, out var r) ? r : UserRole.Student;
            
            var dto = new AuthUserDto
            {
                Id = user.Id,
                Role = roleEnum,
                FullName = user.Name,
                FullNameAr = user.FullNameAr,
                Email = user.Email,
                AvatarUrl = user.AvatarUrl,
                FacultyId = user.FacultyId,
                DepartmentId = user.DepartmentId,
                MustChangePw = user.MustChangePw,
                ProfileComplete = user.ProfileComplete
            };

            if (user.Role != null)
            {
                var features = user.Role.Permissions
                    .Where(p => p.Feature != null)
                    .GroupBy(p => p.Feature)
                    .Select(g => new FeatureDto
                    {
                        Name = g.Key.Name,
                        NameAr = g.Key.NameAr,
                        Permissions = g.Select(p => new PermissionDto
                        {
                            Name = p.Name,
                            NameAr = p.Ar_Name,
                        }).ToList()
                    }).ToList();

                dto.RoleDetails = new RoleDto
                {
                    Name = user.Role.Name,
                    NameAr = user.Role.NameAr,
                    Features = features
                };
            }

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

        private AuthTokensDto GenerateTokens(User user, bool rememberMe)
        {
            var jwtKey = _config["JWT_KEY"] ?? "MasaarVerySecureSuperSecretKey123456!!";
            var issuer = _config["JWT_ISSUER"] ?? "Masaar";
            var audience = _config["JWT_AUDIENCE"] ?? "MasaarClient";

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("role", user.Role?.Name?.ToLower() ?? "student"),
                new Claim("faculty_id", user.FacultyId ?? ""),
                new Claim("department_id", user.DepartmentId ?? "")
            };

            var expiry = DateTime.UtcNow.AddMinutes(15);
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
                RefreshToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
                ExpiresIn = DateTime.UtcNow.AddMinutes(15) // 15 mins
            };

            // Save Refresh Token to DB
            _context.Tokens.Add(new Token
            {
                UserId = user.Id,
                TokenValue = tokens.RefreshToken,
                TokenType = "refresh",
                ExpiryDate = DateTimeOffset.UtcNow.AddDays(7)
            });
            _context.Tokens.Add(new Token
            {
                UserId = user.Id,
                TokenValue = tokens.AccessToken,
                TokenType = "access",
                ExpiryDate = DateTimeOffset.UtcNow.AddMinutes(15)
            });
            _context.SaveChanges();

            return tokens;
        }
    
        private AuthTokensDto GenerateAccessTokens(User user, bool rememberMe, string RefreshToken)
        {
            var jwtKey = _config["JWT_KEY"] ?? "MasaarVerySecureSuperSecretKey123456!!";
            var issuer = _config["JWT_ISSUER"] ?? "Masaar";
            var audience = _config["JWT_AUDIENCE"] ?? "MasaarClient";

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("role", user.Role?.Name?.ToLower() ?? "student"),
                new Claim("faculty_id", user.FacultyId ?? ""),
                new Claim("department_id", user.DepartmentId ?? "")
            };

            var expiry = DateTime.UtcNow.AddMinutes(15);
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
                ExpiresIn = DateTime.UtcNow.AddMinutes(15) // 15 mins
            };

            _context.Tokens.Add(new Token
            {
                UserId = user.Id,
                TokenValue = tokens.AccessToken,
                TokenType = "access",
                ExpiryDate = DateTimeOffset.UtcNow.AddMinutes(15)
            });
            _context.SaveChanges();

            return tokens;
        }
    }
}
