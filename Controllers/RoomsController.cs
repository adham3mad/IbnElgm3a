using IbnElgm3a.DTOs.Rooms;
using IbnElgm3a.Enums;
using IbnElgm3a.Model;
using IbnElgm3a.Model.Data;
using IbnElgm3a.Models;
using IbnElgm3a.Models.Data;
using IbnElgm3a.Filters;
using IbnElgm3a.Services.Localization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IbnElgm3a.Controllers
{
    [ApiController]
    [Route("v1/admin/rooms")]
    [Authorize]
    public class RoomsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILocalizationService _localizer;

        public RoomsController(AppDbContext context, ILocalizationService localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        [HttpGet]
        [RequirePermission(PermissionEnum.Dashboard_StructureRead)]
        public async Task<IActionResult> GetRooms()
        {
            var rooms = await _context.Rooms
                .Select(r => new RoomResponseDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Capacity = r.Capacity
                }).ToListAsync();

            return Ok(ApiResponse<List<RoomResponseDto>>.CreateSuccess(rooms));
        }

        [HttpGet("{id}")]
        [RequirePermission(PermissionEnum.Dashboard_StructureRead)]
        public async Task<IActionResult> GetRoomById(string id)
        {
            var r = await _context.Rooms.FindAsync(id);
            if (r == null) return NotFound(ApiResponse<object>.CreateError("ROOM_NOT_FOUND", "Room not found."));

            return Ok(ApiResponse<RoomResponseDto>.CreateSuccess(new RoomResponseDto
            {
                Id = r.Id,
                Name = r.Name,
                Capacity = r.Capacity
            }));
        }

        [HttpPost]
        [RequirePermission(PermissionEnum.Dashboard_StructureCreate)]
        public async Task<IActionResult> CreateRoom([FromBody] CreateRoomRequestDto request)
        {
            var room = new Room
            {
                Id = "room_" + Guid.NewGuid().ToString("N").Substring(0, 10),
                Name = request.Name,
                Capacity = request.Capacity,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();

            return Created("", ApiResponse<object>.CreateSuccess(new { id = room.Id }));
        }

        [HttpPatch("{id}")]
        [RequirePermission(PermissionEnum.Dashboard_StructureUpdate)]
        public async Task<IActionResult> UpdateRoom(string id, [FromBody] UpdateRoomRequestDto request)
        {
            var r = await _context.Rooms.FindAsync(id);
            if (r == null) return NotFound(ApiResponse<object>.CreateError("ROOM_NOT_FOUND", "Room not found."));

            if (request.Name != null) r.Name = request.Name;
            if (request.Capacity.HasValue) r.Capacity = request.Capacity.Value;
            r.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.CreateSuccess(new { message = _localizer.GetMessage("UPDATED_SUCCESS") }));
        }

        [HttpDelete("{id}")]
        [RequirePermission(PermissionEnum.Dashboard_StructureDelete)]
        public async Task<IActionResult> DeleteRoom(string id)
        {
            var r = await _context.Rooms.FindAsync(id);
            if (r == null) return NotFound(ApiResponse<object>.CreateError("ROOM_NOT_FOUND", "Room not found."));

            // Check if used in schedule slots
            if (await _context.ScheduleSlots.AnyAsync(s => s.RoomId == id))
                return BadRequest(ApiResponse<object>.CreateError("ROOM_IN_USE", "Room is in use by schedule slots."));

            _context.Rooms.Remove(r);
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.CreateSuccess(new { message = _localizer.GetMessage("DELETED_SUCCESS") }));
        }
    }
}
