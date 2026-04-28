using IbnElgm3a.Models;
using IbnElgm3a.DTOs.Calendar;
using IbnElgm3a.Enums;
using IbnElgm3a.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IbnElgm3a.Models;
using IbnElgm3a.Models.Data;
using Microsoft.EntityFrameworkCore;
using IbnElgm3a.Services.Localization;

namespace IbnElgm3a.Controllers.Admins
{
    [ApiController]
    [Route("v1/admin/calendar")]
    [Authorize]
    public class CalendarController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILocalizationService _localizer;

        public CalendarController(AppDbContext context, ILocalizationService localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        [HttpGet]
        [RequirePermission(PermissionEnum.Dashboard_Calendar_Read)]
        public async Task<IActionResult> GetEvents([FromQuery] string? semester_id = null, [FromQuery] string? start_date = null, [FromQuery] string? end_date = null)
        {
            var query = _context.CalendarEvents.AsQueryable();

            if (!string.IsNullOrEmpty(semester_id))
            {
                query = query.Where(e => e.SemesterId == semester_id);
            }

            if (!string.IsNullOrEmpty(start_date) && System.DateTime.TryParse(start_date, out var startDateParsed))
            {
                query = query.Where(e => e.Date >= startDateParsed);
            }

            if (!string.IsNullOrEmpty(end_date) && System.DateTime.TryParse(end_date, out var endDateParsed))
            {
                query = query.Where(e => e.Date <= endDateParsed);
            }

            var events = await query
                .OrderBy(e => e.Date)
                .Select(e => new CalendarEventResponseDto
                {
                    Id = e.Id,
                    Title = e.Title,
                    Date = e.Date,
                    EndDate = e.EndDate,
                    Type = e.Type,
                    IsPublic = e.IsPublic,
                    ColorSeed = e.ColorSeed
                }).ToListAsync();

            return Ok(ApiResponse<List<CalendarEventResponseDto>>.CreateSuccess(events));
        }

        [HttpPost]
        [RequirePermission(PermissionEnum.Dashboard_Calendar_Create)]
        public async Task<IActionResult> CreateEvent([FromBody] CreateCalendarEventRequestDto request)
        {
            if (!string.IsNullOrEmpty(request.SemesterId))
            {
                var semExists = await _context.Semesters.AnyAsync(s => s.Id == request.SemesterId);
                if (!semExists) return BadRequest(ApiResponse<object>.CreateError("SEMESTER_NOT_FOUND", _localizer.GetMessage("SEMESTER_NOT_FOUND")));
            }

            var calendarEvent = new CalendarEvent
            {
                Id = "evt_" + System.Guid.NewGuid().ToString("N").Substring(0, 10),
                Title = request.Title,
                Date = request.Date,
                EndDate = request.EndDate,
                Type = request.Type,
                SemesterId = request.SemesterId,
                IsPublic = request.IsPublic,
                ColorSeed = "blue"
            };

            _context.CalendarEvents.Add(calendarEvent);
            await _context.SaveChangesAsync();

            return Created("", ApiResponse<object>.CreateSuccess(new { id = calendarEvent.Id }));
        }

        [HttpPatch("{event_id}")]
        [RequirePermission(PermissionEnum.Dashboard_Calendar_Update)]
        public async Task<IActionResult> UpdateEvent(string event_id, [FromBody] UpdateCalendarEventRequestDto request)
        {
            var e = await _context.CalendarEvents.FindAsync(event_id);
            if (e == null) return NotFound(ApiResponse<object>.CreateError("EVENT_NOT_FOUND", _localizer.GetMessage("EVENT_NOT_FOUND")));

            if (!string.IsNullOrEmpty(request.Title)) e.Title = request.Title;
            if (request.Date.HasValue) e.Date = request.Date.Value;
            if (request.EndDate.HasValue) e.EndDate = request.EndDate.Value;
            if (request.Type.HasValue) e.Type = request.Type.Value;
            if (request.Description != null) e.Description = request.Description;
            if (request.IsPublic.HasValue) e.IsPublic = request.IsPublic.Value;
            if (!string.IsNullOrEmpty(request.SemesterId))
            {
                var semExists = await _context.Semesters.AnyAsync(s => s.Id == request.SemesterId);
                if (!semExists) return BadRequest(ApiResponse<object>.CreateError("SEMESTER_NOT_FOUND", _localizer.GetMessage("SEMESTER_NOT_FOUND")));
                e.SemesterId = request.SemesterId;
            }

            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.CreateSuccess(new { message = _localizer.GetMessage("UPDATED_SUCCESS") }));
        }

        [HttpDelete("{event_id}")]
        [RequirePermission(PermissionEnum.Dashboard_Calendar_Delete)]
        public async Task<IActionResult> DeleteEvent(string event_id)
        {
            var calendarEvent = await _context.CalendarEvents.FindAsync(event_id);
            if (calendarEvent == null) return NotFound(ApiResponse<object>.CreateError("EVENT_NOT_FOUND", _localizer.GetMessage("EVENT_NOT_FOUND")));

            _context.CalendarEvents.Remove(calendarEvent);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.CreateSuccess(new { message = _localizer.GetMessage("DELETED_SUCCESS") }));
        }
    }
}
