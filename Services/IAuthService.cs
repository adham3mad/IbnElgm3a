using IbnElgm3a.DTOs.Auth;
using System.Threading.Tasks;

namespace IbnElgm3a.Services
{
    public interface IAuthService
    {
        Task<LoginResponseDto?> LoginAsync(LoginRequestDto request);
        Task<AuthTokensDto?> RefreshTokenAsync(RefreshRequestDto request);
        Task<bool> LogoutAsync(LogoutRequestDto request, string userId);
        
        Task<(string Channel, int ExpiresIn)?> RequestForgotPasswordAsync(ForgotPasswordRequestDto request);
        Task<string?> VerifyOtpAsync(VerifyOtpRequestDto request);
        Task<bool> ResetPasswordAsync(ResetPasswordRequestDto request);

        Task<LoginResponseDto?> BiometricLoginAsync(BiometricLoginRequestDto request);
        Task<bool> RegisterBiometricAsync(BiometricRegisterRequestDto request, string userId);
    }
}
