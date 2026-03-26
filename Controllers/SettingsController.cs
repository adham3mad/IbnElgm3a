using IbnElgm3a.DTOs.Settings;
using IbnElgm3a.Filters;
using IbnElgm3a.Models;
using IbnElgm3a.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using IbnElgm3a.Model;
using IbnElgm3a.Model.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System;

namespace IbnElgm3a.Controllers
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
        [RequirePermission(PermissionEnum.Dashboard_SettingsRead)]
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
        [RequirePermission(PermissionEnum.Dashboard_SettingsUpdate)]
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
    }
}
