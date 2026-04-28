using IbnElgm3a.DTOs.Settings;
using IbnElgm3a.Filters;
using IbnElgm3a.Models;
using IbnElgm3a.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using IbnElgm3a.Models;
using IbnElgm3a.Models.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System;

namespace IbnElgm3a.Controllers.Admins
{
    [ApiController]
    [Route("v1/admin/settings")]
    [Authorize]
    public class SettingsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SettingsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [RequirePermission(PermissionEnum.Dashboard_Settings_Read)]
        public async Task<IActionResult> GetSettings()
        {
            var settings = await _context.SystemSettings.ToListAsync();
            
            var resp = new SystemSettingsResponseDto();
            
            foreach (var s in settings)
            {
                switch (s.Key)
                {
                    case "maintenance_mode": resp.MaintenanceMode = s.ValueJson == "true"; break;
                    case "allow_registration": resp.AllowRegistration = s.ValueJson == "true"; break;
                    case "current_semester_id": resp.CurrentSemesterId = s.ValueJson; break;
                    case "contact_email": resp.ContactEmail = s.ValueJson; break;
                }
            }

            return Ok(ApiResponse<SystemSettingsResponseDto>.CreateSuccess(resp));
        }

        [HttpPatch]
        [RequirePermission(PermissionEnum.Dashboard_Settings_Update)]
        public async Task<IActionResult> UpdateSettings([FromBody] UpdateSystemSettingsRequestDto request)
        {
            await UpdateSetting("maintenance_mode", request.MaintenanceMode?.ToString().ToLower());
            await UpdateSetting("allow_registration", request.AllowRegistration?.ToString().ToLower());
            await UpdateSetting("current_semester_id", request.CurrentSemesterId);
            await UpdateSetting("contact_email", request.ContactEmail);

            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.CreateSuccess(new { message = "Settings updated successfully" }));
        }

        private async Task UpdateSetting(string key, string? value)
        {
            if (value == null) return;

            var setting = await _context.SystemSettings.FindAsync(key);
            if (setting == null)
            {
                setting = new SystemSetting { Key = key, ValueJson = value, UpdatedAt = DateTimeOffset.UtcNow };
                _context.SystemSettings.Add(setting);
            }
            else
            {
                setting.ValueJson = value;
                setting.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }
        [HttpGet("csv-import-preview")]
        [RequirePermission(PermissionEnum.Dashboard_Settings_Read)]
        public async Task<IActionResult> GetCsvImportPreview([FromQuery] string file_url, [FromQuery] string type)
        {
            await Task.Yield();
            // In a real app, we would download the file from file_url
            // For this implementation, we will mock the validation result
            
            var summary = new
            {
                total_rows = 150,
                valid_rows = 142,
                invalid_rows = 8,
                error_summary = new[]
                {
                    new { row = 12, column = "email", error = "Invalid email format" },
                    new { row = 45, column = "faculty_code", error = "Faculty code not found" }
                }
            };

            var previewRows = new System.Collections.Generic.List<object>();
            for (int i = 0; i < 5; i++)
            {
                if (type == "students")
                {
                    previewRows.Add(new { national_id = $"ID{i}", email = $"student{i}@univ.edu", name = $"Student {i}", faculty_code = "ENG", dept_code = "CS", academic_number = $"2024{i}" });
                }
                else
                {
                    previewRows.Add(new { national_id = $"ID{i}", email = $"instr{i}@univ.edu", name = $"Instructor {i}", faculty_code = "ENG", dept_code = "CS" });
                }
            }

            return Ok(new
            {
                validation_summary = summary,
                preview_rows = previewRows
            });
        }
        [HttpPost("biometric")]
        [RequirePermission(PermissionEnum.Dashboard_Settings_Update)]
        public async Task<IActionResult> ToggleBiometric([FromBody] JsonElement request)
        {
            var enabled = request.GetProperty("enabled").GetBoolean();
            await UpdateSetting("biometric_auth_enabled", enabled.ToString().ToLower());
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.CreateSuccess(new { message = $"Biometric auth {(enabled ? "enabled" : "disabled")}" }));
        }

        [HttpPost("2fa")]
        [RequirePermission(PermissionEnum.Dashboard_Settings_Update)]
        public async Task<IActionResult> Toggle2FA([FromBody] JsonElement request)
        {
            var enabled = request.GetProperty("enabled").GetBoolean();
            await UpdateSetting("two_factor_enabled", enabled.ToString().ToLower());
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.CreateSuccess(new { message = $"2FA {(enabled ? "enabled" : "disabled")}" }));
        }

        [HttpPost("email-verification")]
        [RequirePermission(PermissionEnum.Dashboard_Settings_Update)]
        public async Task<IActionResult> ToggleEmailVerification([FromBody] JsonElement request)
        {
            var enabled = request.GetProperty("enabled").GetBoolean();
            await UpdateSetting("email_verification_required", enabled.ToString().ToLower());
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.CreateSuccess(new { message = $"Email verification {(enabled ? "enabled" : "disabled")}" }));
        }

        [HttpPost("force-reset")]
        [RequirePermission(PermissionEnum.Dashboard_Settings_Update)]
        public async Task<IActionResult> ToggleForceReset([FromBody] JsonElement request)
        {
            var enabled = request.GetProperty("enabled").GetBoolean();
            await UpdateSetting("force_password_reset_enabled", enabled.ToString().ToLower());
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.CreateSuccess(new { message = $"Force password reset {(enabled ? "enabled" : "disabled")}" }));
        }

        [HttpGet("billing-summary")]
        [RequirePermission(PermissionEnum.Dashboard_Settings_Read)]
        public async Task<IActionResult> GetBillingSummary()
        {
            await Task.Yield(); 
            var summary = new
            {
                current_plan = "Enterprise",
                next_invoice = DateTimeOffset.UtcNow.AddMonths(1),
                amount_due = 450.00,
                storage_usage = "45GB / 100GB"
            };
            return Ok(ApiResponse<object>.CreateSuccess(summary));
        }
    }
}
