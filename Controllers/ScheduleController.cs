using IbnElgm3a.Models;
using IbnElgm3a.DTOs.Schedules;
using IbnElgm3a.DTOs.Common;
using IbnElgm3a.DTOs.Rooms;
using IbnElgm3a.Enums;
using IbnElgm3a.Filters;
using IbnElgm3a.Services.Localization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IbnElgm3a.Models;
using IbnElgm3a.Models.Data;
using Microsoft.EntityFrameworkCore;

namespace IbnElgm3a.Controllers
{
    [ApiController]
    [Route("v1/admin/schedule")]
    [Authorize]
    public class ScheduleController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILocalizationService _localizer;

        public ScheduleController(AppDbContext context, ILocalizationService localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        [HttpGet("slots")]
        [RequirePermission(PermissionEnum.Dashboard_Schedule_Read)]
        public async Task<IActionResult> GetSlots(
            [FromQuery] string? semester_id = null,
            [FromQuery] DayOfWeekEnum? day = null,
            [FromQuery] string? room_id = null,
            [FromQuery] string? faculty_id = null)
        {
            var query = _context.ScheduleSlots
                .Include(s => s.Room)
                .Include(s => s.CourseSection)
                    .ThenInclude(sec => sec!.Course)
                .Include(s => s.CourseSection)
                    .ThenInclude(sec => sec!.Instructor)
                        .ThenInclude(i => i!.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(semester_id)) query = query.Where(s => s.SemesterId == semester_id);
            if (day.HasValue) query = query.Where(s => s.Day == day.Value);
            if (!string.IsNullOrEmpty(room_id)) query = query.Where(s => s.RoomId == room_id);
            if (!string.IsNullOrEmpty(faculty_id)) query = query.Where(s => s.Room != null && s.Room.FacultyId == faculty_id);

            var slots = await query
                .OrderBy(s => s.Day).ThenBy(s => s.StartTime)
                .Select(s => new ScheduleSlotResponseDto
                {
                    Id = s.Id,
                    Course = s.CourseSection != null && s.CourseSection.Course != null ? new IdNameDto { Id = s.CourseSection.Course.Id, Name = s.CourseSection.Course.Title } : null,
                    Section = s.CourseSection != null ? new IdNameDto { Id = s.CourseSection.Id, Name = s.CourseSection.Name } : null,
                    Instructor = s.CourseSection != null && s.CourseSection.Instructor != null && s.CourseSection.Instructor.User != null 
                        ? new IdNameDto { Id = s.CourseSection.Instructor.UserId, Name = s.CourseSection.Instructor.User.Name } 
                        : null,
                    Room = s.Room != null ? new RoomResponseDto { Id = s.Room.Id, Name = s.Room.Name, Capacity = s.Room.Capacity } : null,
                    Day = s.Day,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    Type = s.Type,
                    Conflict = false // Conflict detail logic placeholder
                }).ToListAsync();

            return Ok(ApiResponse<List<ScheduleSlotResponseDto>>.CreateSuccess(slots));
        }

        [HttpPost("slots")]
        [RequirePermission(PermissionEnum.Dashboard_Schedule_Create)]
        public async Task<IActionResult> CreateScheduleSlot([FromBody] CreateScheduleSlotRequestDto request)
        {
            // Uniqueness/Conflict check logic
            var conflict = await _context.ScheduleSlots.AnyAsync(s => 
                s.SemesterId == request.SemesterId && 
                s.RoomId == request.RoomId && 
                s.Day == request.Day && 
                ((string.Compare(s.StartTime, request.StartTime) <= 0 && string.Compare(s.EndTime, request.StartTime) > 0) || 
                 (string.Compare(s.StartTime, request.EndTime) < 0 && string.Compare(s.EndTime, request.EndTime) >= 0)));

            if (conflict) return BadRequest(ApiResponse<object>.CreateError("SCHEDULE_CONFLICT", _localizer.GetMessage("SCHEDULE_CONFLICT")));

            var slot = new ScheduleSlot
            {
                Id = "slot_" + System.Guid.NewGuid().ToString("N").Substring(0, 10),
                CourseSectionId = request.CourseSectionId,
                RoomId = request.RoomId,
                Day = request.Day,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                Type = request.Type,
                Recurrence = request.Recurrence,
                SemesterId = request.SemesterId
            };

            _context.ScheduleSlots.Add(slot);
            await _context.SaveChangesAsync();

            return Created("", ApiResponse<object>.CreateSuccess(new { id = slot.Id }));
        }

        [HttpPatch("slots/{slot_id}")]
        [RequirePermission(PermissionEnum.Dashboard_Schedule_Update)]
        public async Task<IActionResult> UpdateSlot(string slot_id, [FromBody] UpdateScheduleSlotRequestDto request)
        {
            var slot = await _context.ScheduleSlots.FindAsync(slot_id);
            if (slot == null) return NotFound(ApiResponse<object>.CreateError("SLOT_NOT_FOUND", _localizer.GetMessage("SLOT_NOT_FOUND")));

            if (!string.IsNullOrEmpty(request.CourseSectionId)) slot.CourseSectionId = request.CourseSectionId;
            if (!string.IsNullOrEmpty(request.RoomId)) slot.RoomId = request.RoomId;
            if (request.Day.HasValue) slot.Day = request.Day.Value;
            if (!string.IsNullOrEmpty(request.StartTime)) slot.StartTime = request.StartTime;
            if (!string.IsNullOrEmpty(request.EndTime)) slot.EndTime = request.EndTime;
            if (request.Type.HasValue) slot.Type = request.Type.Value;
            if (request.Recurrence.HasValue) slot.Recurrence = request.Recurrence.Value;
            if (!string.IsNullOrEmpty(request.SemesterId)) slot.SemesterId = request.SemesterId;

            // Optional: Re-check conflict here after updates
            var conflict = await _context.ScheduleSlots.AnyAsync(s => 
                s.Id != slot_id &&
                s.SemesterId == slot.SemesterId && 
                s.RoomId == slot.RoomId && 
                s.Day == slot.Day && 
                ((string.Compare(s.StartTime, slot.StartTime) <= 0 && string.Compare(s.EndTime, slot.StartTime) > 0) || 
                 (string.Compare(s.StartTime, slot.EndTime) < 0 && string.Compare(s.EndTime, slot.EndTime) >= 0)));

            if (conflict) return BadRequest(ApiResponse<object>.CreateError("SCHEDULE_CONFLICT", _localizer.GetMessage("SCHEDULE_CONFLICT")));

            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.CreateSuccess(new { message = _localizer.GetMessage("UPDATED_SUCCESS") }));
        }

        [HttpDelete("slots/{slot_id}")]
        [RequirePermission(PermissionEnum.Dashboard_Schedule_Delete)]
        public async Task<IActionResult> DeleteSlot(string slot_id)
        {
            var slot = await _context.ScheduleSlots.FindAsync(slot_id);
            if (slot == null) return NotFound(ApiResponse<object>.CreateError("SLOT_NOT_FOUND", _localizer.GetMessage("SLOT_NOT_FOUND")));

            _context.ScheduleSlots.Remove(slot);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.CreateSuccess(new { message = _localizer.GetMessage("DELETED_SUCCESS") }));
        }
        [HttpGet("conflict-resolution")]
        [RequirePermission(PermissionEnum.Dashboard_Schedule_Read)]
        public async Task<IActionResult> GetConflictResolution(
            [FromQuery] string? slot_id = null,
            [FromQuery] DayOfWeekEnum? day = null,
            [FromQuery] string? start_time = null,
            [FromQuery] string? end_time = null,
            [FromQuery] string? room_id = null,
            [FromQuery] string? section_id = null)
        {
            if (!day.HasValue || string.IsNullOrEmpty(start_time) || string.IsNullOrEmpty(end_time))
                return BadRequest(ApiResponse<object>.CreateError("MISSING_PARAMS", "Day, start_time, and end_time are required"));

            var roomConflicts = new List<object>();
            var instructorConflicts = new List<object>();

            // 1. Room Conflict
            if (!string.IsNullOrEmpty(room_id))
            {
                var rConflicts = await _context.ScheduleSlots
                    .Include(s => s.CourseSection)
                        .ThenInclude(sec => sec!.Course)
                    .Where(s => s.Id != slot_id && s.RoomId == room_id && s.Day == day.Value &&
                        ((string.Compare(s.StartTime, start_time) < 0 && string.Compare(s.EndTime, start_time) > 0) ||
                         (string.Compare(s.StartTime, end_time) < 0 && string.Compare(s.EndTime, end_time) > 0) ||
                         (string.Compare(s.StartTime, start_time) >= 0 && string.Compare(s.EndTime, end_time) <= 0)))
                    .ToListAsync();

                foreach (var c in rConflicts)
                {
                    roomConflicts.Add(new { type = "room_overlap", slot_id = c.Id, course_name = c.CourseSection?.Course?.Title, room_name = c.Room?.Name });
                }
            }

            // 2. Instructor Conflict
            if (!string.IsNullOrEmpty(section_id))
            {
                var section = await _context.Sections.FindAsync(section_id);
                if (section != null && !string.IsNullOrEmpty(section.InstructorId))
                {
                    var iConflicts = await _context.ScheduleSlots
                        .Include(s => s.CourseSection)
                            .ThenInclude(sec => sec!.Course)
                        .Where(s => s.Id != slot_id && s.Day == day.Value && s.CourseSection != null && s.CourseSection.InstructorId == section.InstructorId &&
                            ((string.Compare(s.StartTime, start_time) < 0 && string.Compare(s.EndTime, start_time) > 0) ||
                             (string.Compare(s.StartTime, end_time) < 0 && string.Compare(s.EndTime, end_time) > 0) ||
                             (string.Compare(s.StartTime, start_time) >= 0 && string.Compare(s.EndTime, end_time) <= 0)))
                        .ToListAsync();

                    foreach (var c in iConflicts)
                    {
                        instructorConflicts.Add(new { type = "instructor_busy", slot_id = c.Id, course_name = c.CourseSection?.Course?.Title, instructor_name = c.CourseSection?.Instructor?.UserId });
                    }
                }
            }

            return Ok(new
            {
                has_conflict = roomConflicts.Any() || instructorConflicts.Any(),
                room_conflicts = roomConflicts,
                instructor_conflicts = instructorConflicts
            });
        }
    }
}
