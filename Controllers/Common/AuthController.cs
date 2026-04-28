using IbnElgm3a.DTOs.Auth;
using IbnElgm3a.Enums;
using IbnElgm3a.Models;
using IbnElgm3a.Services;
using IbnElgm3a.Services.Localization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace IbnElgm3a.Controllers.Common
{
    [ApiController]
    [Route("v1/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILocalizationService _localizer;

        public AuthController(IAuthService authService, ILocalizationService localizer)
        {
            _authService = authService;
            _localizer = localizer;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            var result = await _authService.LoginAsync(request);
            if (result == null)
            {
                return Unauthorized(ApiResponse<object>.CreateError(
                    "INVALID_CREDENTIALS",
                    _localizer.GetMessage("INVALID_CREDENTIALS")
                ));
            }

            return Ok(result);
        }

        [HttpPost("refresh")]
        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequestDto request)
        {
            var result = await _authService.RefreshTokenAsync(request);
            if (result == null)
            {
                return Unauthorized(ApiResponse<object>.CreateError("INVALID_TOKEN", _localizer.GetMessage("INVALID_TOKEN")));
            }

            return Ok(result);
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] LogoutRequestDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            await _authService.LogoutAsync(request, userId);
            return NoContent();
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
        {
            var result = await _authService.RequestForgotPasswordAsync(request);
            if (result == null)
            {
                // Always return Ok to prevent email enumeration.
                return Ok(new { channel = request.Channel, expires_in = 900 });
            }

            return Ok(new
            {
                channel = result.Value.Channel,
                expires_in = result.Value.ExpiresIn
            });
        }

        [HttpPost("verify-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequestDto request)
        {
            var resetToken = await _authService.VerifyOtpAsync(request);
            if (resetToken == null)
            {
                return BadRequest(ApiResponse<object>.CreateError("INVALID_OTP", _localizer.GetMessage("INVALID_OTP")));
            }

            return Ok(new { reset_token = resetToken });
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
        {
            var success = await _authService.ResetPasswordAsync(request);
            if (!success)
            {
                return BadRequest(ApiResponse<object>.CreateError("INVALID_TOKEN", _localizer.GetMessage("INVALID_TOKEN")));
            }

            return Ok(new { message = _localizer.GetMessage("PASSWORD_UPDATED") });
        }

        [HttpGet("biometric/challenge")]
        [AllowAnonymous]
        public async Task<IActionResult> GetBiometricChallenge()
        {
            var challenge = await _authService.GetBiometricChallengeAsync();
            return Ok(new { challenge });
        }

        [HttpPost("biometric/login")]
        [AllowAnonymous]
        public async Task<IActionResult> BiometricLogin([FromBody] BiometricLoginRequestDto request)
        {
            var result = await _authService.BiometricLoginAsync(request);
            if (result == null)
            {
                return Unauthorized(ApiResponse<object>.CreateError("INVALID_SIGNATURE", _localizer.GetMessage("INVALID_SIGNATURE")));
            }

            return Ok(result);
        }

        [HttpPost("biometric/register")]
        [Authorize]
        public async Task<IActionResult> RegisterBiometric([FromBody] BiometricRegisterRequestDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var success = await _authService.RegisterBiometricAsync(request, userId);
            if (!success)
            {
                return BadRequest(ApiResponse<object>.CreateError("REGISTRATION_FAILED", _localizer.GetMessage("REGISTRATION_FAILED")));
            }

            return Created("", new { device_id = request.DeviceId, registered_at = System.DateTimeOffset.UtcNow });
        }
    }
}
