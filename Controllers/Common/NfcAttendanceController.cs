using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using IbnElgm3a.Filters;
using IbnElgm3a.Attributes;
using IbnElgm3a.DTOs.Nfc;
using IbnElgm3a.Enums;
using IbnElgm3a.Models;
using IbnElgm3a.Models.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using IbnElgm3a.Services.Localization;

namespace IbnElgm3a.Controllers.Common
{
    [ApiController]
    [AllowAnonymous]
    [BypassResponseWrapper]
    public class NfcAttendanceController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILocalizationService _localizer;
        private const string UidRegexPattern = @"^([0-9A-F]{2}:)*[0-9A-F]{2}$";

        public NfcAttendanceController(AppDbContext context, IMemoryCache cache, ILocalizationService localizer)
        {
            _context = context;
            _cache = cache;
            _localizer = localizer;
        }

        // Helper to validate common request parameters
        private async Task<(bool IsValid, IActionResult? ErrorResult, Card? CardRecord)> ValidateScanAsync(
            NfcBaseRequest request, 
            string endpoint, 
            bool skipCardLookup = false)
        {
            // 1. Shared Secret Validation
            var serverSecret = Environment.GetEnvironmentVariable("ESP32_SECRET");
            if (string.IsNullOrEmpty(serverSecret) || request.Secret != serverSecret)
            {
                await LogAuditAsync(request.Uid, request.DeviceId, endpoint, 401, "error");
                return (false, StatusCode(401, new { error = _localizer.GetMessage("NFC_INVALID_SECRET") }), null);
            }

            // 2. UID Regex Validation
            if (string.IsNullOrEmpty(request.Uid) || !Regex.IsMatch(request.Uid, UidRegexPattern, RegexOptions.IgnoreCase))
            {
                await LogAuditAsync(request.Uid ?? "", request.DeviceId, endpoint, 422, "error");
                return (false, StatusCode(422, new { error = _localizer.GetMessage("NFC_INVALID_UID_FORMAT") }), null);
            }

            // 3. Rate Limiting (1 request per card UID per 3 seconds)
            var cacheKey = $"nfc_cooldown:{request.Uid}:{endpoint}";
            if (_cache.TryGetValue(cacheKey, out _))
            {
                await LogAuditAsync(request.Uid, request.DeviceId, endpoint, 429, "denied");
                return (false, StatusCode(429, new { error = _localizer.GetMessage("NFC_RATE_LIMITED") }), null);
            }

            // Set cooldown in cache for 3 seconds
            _cache.Set(cacheKey, true, TimeSpan.FromSeconds(3));

            if (skipCardLookup)
            {
                return (true, null, null);
            }

            // 4. Card Lookup
            var card = await _context.Cards
                .Include(c => c.Student)
                .ThenInclude(s => s!.User)
                .FirstOrDefaultAsync(c => c.Uid == request.Uid);

            if (card == null)
            {
                await LogAuditAsync(request.Uid, request.DeviceId, endpoint, 404, "denied");
                return (false, NotFound(new 
                { 
                    allowed = false, 
                    message = _localizer.GetMessage("NFC_CARD_NOT_FOUND_MSG"), 
                    sub = _localizer.GetMessage("NFC_CARD_NOT_FOUND_SUB"), 
                    error = _localizer.GetMessage("NFC_CARD_NOT_FOUND_ERR") 
                }), null);
            }

            // 5. Card Status check
            if (card.Status != "active")
            {
                await LogAuditAsync(request.Uid, request.DeviceId, endpoint, 403, "denied");
                return (false, StatusCode(403, new 
                { 
                    allowed = false, 
                    message = _localizer.GetMessage("NFC_CARD_BLOCKED_MSG"), 
                    sub = _localizer.GetMessage("NFC_CARD_BLOCKED_SUB"), 
                    error = _localizer.GetMessage("NFC_CARD_BLOCKED_ERR") 
                }), card);
            }

            return (true, null, card);
        }

        private async Task LogAuditAsync(string uid, string deviceId, string endpoint, int httpStatus, string result)
        {
            var audit = new ScanAudit
            {
                Uid = uid,
                DeviceId = deviceId ?? "unknown",
                Endpoint = endpoint,
                HttpStatus = httpStatus,
                Result = result,
                ScannedAt = DateTimeOffset.UtcNow
            };
            _context.ScanAudits.Add(audit);
            await _context.SaveChangesAsync();
        }

        [HttpPost("attendance/entry")]
        public async Task<IActionResult> EntryScan([FromBody] NfcBaseRequest request)
        {
            var (isValid, errorResult, card) = await ValidateScanAsync(request, "/attendance/entry");
            if (!isValid) return errorResult!;

            if (card!.StudentId == null || card.Student == null)
            {
                await LogAuditAsync(request.Uid, request.DeviceId, "/attendance/entry", 404, "denied");
                return NotFound(new 
                { 
                    allowed = false, 
                    message = _localizer.GetMessage("NFC_CARD_NOT_LINKED_MSG"), 
                    sub = _localizer.GetMessage("NFC_CARD_NOT_LINKED_SUB"), 
                    error = _localizer.GetMessage("NFC_CARD_NOT_LINKED_ERR") 
                });
            }

            var student = card.Student;
            var today = DateTimeOffset.UtcNow.Date;

            // Check for existing open campus session today
            var openSession = await _context.CampusSessions
                .FirstOrDefaultAsync(cs => cs.StudentId == student.Id && cs.ExitTime == null && cs.EntryTime.Date == today);

            if (openSession != null)
            {
                await LogAuditAsync(request.Uid, request.DeviceId, "/attendance/entry", 409, "denied");
                return Conflict(new 
                { 
                    allowed = false, 
                    message = _localizer.GetMessage("NFC_ALREADY_IN_CAMPUS_MSG"), 
                    sub = _localizer.GetMessage("NFC_ALREADY_IN_CAMPUS_SUB"), 
                    error = _localizer.GetMessage("NFC_ALREADY_IN_CAMPUS_ERR") 
                });
            }

            // Create new session
            var session = new CampusSession
            {
                StudentId = student.Id,
                DeviceId = request.DeviceId,
                EntryTime = DateTimeOffset.UtcNow,
                ExitTime = null
            };

            _context.CampusSessions.Add(session);
            await LogAuditAsync(request.Uid, request.DeviceId, "/attendance/entry", 200, "granted");
            await _context.SaveChangesAsync();

            return Ok(new
            {
                allowed = true,
                message = _localizer.GetMessage("NFC_WELCOME_MSG"),
                sub = student.User?.Name ?? "Student"
            });
        }

        [HttpPost("attendance/exit")]
        public async Task<IActionResult> ExitScan([FromBody] NfcBaseRequest request)
        {
            var (isValid, errorResult, card) = await ValidateScanAsync(request, "/attendance/exit");
            if (!isValid) return errorResult!;

            if (card!.StudentId == null || card.Student == null)
            {
                await LogAuditAsync(request.Uid, request.DeviceId, "/attendance/exit", 404, "denied");
                return NotFound(new 
                { 
                    allowed = false, 
                    message = _localizer.GetMessage("NFC_CARD_NOT_LINKED_MSG"), 
                    sub = _localizer.GetMessage("NFC_CARD_NOT_LINKED_SUB"), 
                    error = _localizer.GetMessage("NFC_CARD_NOT_LINKED_ERR") 
                });
            }

            var student = card.Student;
            var today = DateTimeOffset.UtcNow.Date;

            // Find most recent open campus session for today
            var openSession = await _context.CampusSessions
                .OrderByDescending(cs => cs.EntryTime)
                .FirstOrDefaultAsync(cs => cs.StudentId == student.Id && cs.ExitTime == null && cs.EntryTime.Date == today);

            if (openSession == null)
            {
                // Anomaly: Student exits without entry record. Log and still return 200.
                await LogAuditAsync(request.Uid, request.DeviceId, "/attendance/exit", 200, "granted");
                return Ok(new
                {
                    allowed = true,
                    message = _localizer.GetMessage("NFC_GOODBYE_MSG"),
                    sub = student.User?.Name ?? "Student"
                });
            }

            openSession.ExitTime = DateTimeOffset.UtcNow;
            await LogAuditAsync(request.Uid, request.DeviceId, "/attendance/exit", 200, "granted");
            await _context.SaveChangesAsync();

            return Ok(new
            {
                allowed = true,
                message = _localizer.GetMessage("NFC_GOODBYE_MSG"),
                sub = student.User?.Name ?? "Student"
            });
        }

        [HttpPost("attendance/room")]
        public async Task<IActionResult> RoomScan([FromBody] NfcRoomRequest request)
        {
            var (isValid, errorResult, card) = await ValidateScanAsync(request, "/attendance/room");
            if (!isValid) return errorResult!;

            if (card!.StudentId == null || card.Student == null)
            {
                await LogAuditAsync(request.Uid, request.DeviceId, "/attendance/room", 404, "denied");
                return NotFound(new 
                { 
                    allowed = false, 
                    message = _localizer.GetMessage("NFC_CARD_NOT_LINKED_MSG"), 
                    sub = _localizer.GetMessage("NFC_CARD_NOT_LINKED_SUB"), 
                    error = _localizer.GetMessage("NFC_CARD_NOT_LINKED_ERR") 
                });
            }

            var student = card.Student;
            var today = DateTimeOffset.UtcNow.Date;

            // 1. Gate check: query campus_sessions for today's open entry session
            var hasOpenSession = await _context.CampusSessions
                .AnyAsync(cs => cs.StudentId == student.Id && cs.ExitTime == null && cs.EntryTime.Date == today);

            if (!hasOpenSession)
            {
                await LogAuditAsync(request.Uid, request.DeviceId, "/attendance/room", 403, "denied");
                return StatusCode(403, new { error = _localizer.GetMessage("NFC_NO_ENTRY_SESSION") });
            }

            // 2. Room lookup
            var roomIdStr = request.RoomId.ToString();
            var room = await _context.Rooms
                .FirstOrDefaultAsync(r => r.Code == roomIdStr || r.Id == roomIdStr);

            if (room == null)
            {
                await LogAuditAsync(request.Uid, request.DeviceId, "/attendance/room", 404, "denied");
                return NotFound(new 
                { 
                    allowed = false, 
                    message = _localizer.GetMessage("NFC_ROOM_NOT_FOUND_MSG"), 
                    sub = _localizer.GetMessage("NFC_ROOM_NOT_FOUND_SUB"), 
                    error = _localizer.GetMessage("NFC_ROOM_NOT_FOUND_ERR") 
                });
            }

            // 3. Active Schedule Entry lookup (±15 min window on current day)
            var localTime = DateTime.Now;
            var dayEnum = localTime.DayOfWeek switch
            {
                DayOfWeek.Saturday => DayOfWeekEnum.Saturday,
                DayOfWeek.Sunday => DayOfWeekEnum.Sunday,
                DayOfWeek.Monday => DayOfWeekEnum.Monday,
                DayOfWeek.Tuesday => DayOfWeekEnum.Tuesday,
                DayOfWeek.Wednesday => DayOfWeekEnum.Wednesday,
                DayOfWeek.Thursday => DayOfWeekEnum.Thursday,
                DayOfWeek.Friday => DayOfWeekEnum.Friday,
                _ => DayOfWeekEnum.Saturday
            };

            var slots = await _context.ScheduleSlots
                .Where(s => s.RoomId == room.Id && s.Day == dayEnum)
                .ToListAsync();

            ScheduleSlot? activeSlot = null;
            var currentLocalTime = localTime.TimeOfDay;
            foreach (var slot in slots)
            {
                if (TimeSpan.TryParse(slot.StartTime, out var slotStart) && TimeSpan.TryParse(slot.EndTime, out var slotEnd))
                {
                    var windowStart = slotStart.Add(TimeSpan.FromMinutes(-15));
                    var windowEnd = slotEnd.Add(TimeSpan.FromMinutes(15));
                    if (currentLocalTime >= windowStart && currentLocalTime <= windowEnd)
                    {
                        activeSlot = slot;
                        break;
                    }
                }
            }

            if (activeSlot == null)
            {
                await LogAuditAsync(request.Uid, request.DeviceId, "/attendance/room", 404, "denied");
                return NotFound(new 
                { 
                    allowed = false, 
                    message = _localizer.GetMessage("NFC_NO_CLASS_ACTIVE_MSG"), 
                    sub = _localizer.GetMessage("NFC_NO_CLASS_ACTIVE_SUB"), 
                    error = _localizer.GetMessage("NFC_NO_CLASS_ACTIVE_ERR") 
                });
            }

            // Get course name from schedule slot section
            var section = await _context.Sections
                .Include(sec => sec.Course)
                .FirstOrDefaultAsync(sec => sec.Id == activeSlot.SectionId);

            var courseName = section?.Course?.Title ?? "Lecture";

            // 4. Check for duplicate scan today
            var hasRegisteredToday = await _context.RoomAttendances
                .AnyAsync(ra => ra.StudentId == student.Id && ra.RoomId == room.Id && ra.ScannedAt.Date == today);

            if (hasRegisteredToday)
            {
                await LogAuditAsync(request.Uid, request.DeviceId, "/attendance/room", 409, "denied");
                return Conflict(new 
                { 
                    allowed = false, 
                    message = _localizer.GetMessage("NFC_ALREADY_IN_ROOM_MSG"), 
                    sub = _localizer.GetMessage("NFC_ALREADY_IN_ROOM_SUB"), 
                    error = _localizer.GetMessage("NFC_ALREADY_IN_ROOM_ERR") 
                });
            }

            // 5. Insert room attendance record
            var attendance = new RoomAttendance
            {
                StudentId = student.Id,
                RoomId = room.Id,
                DeviceId = request.DeviceId,
                ScannedAt = DateTimeOffset.UtcNow
            };

            _context.RoomAttendances.Add(attendance);
            await LogAuditAsync(request.Uid, request.DeviceId, "/attendance/room", 200, "granted");
            await _context.SaveChangesAsync();

            return Ok(new
            {
                allowed = true,
                message = _localizer.GetMessage("NFC_REGISTERED_SUCCESS"),
                sub = courseName.Length > 21 ? courseName.Substring(0, 18) + "..." : courseName
            });
        }

        [HttpPost("admin/card")]
        [RequirePermission(PermissionEnum.manage_cards)]
        public async Task<IActionResult> AdminCardScan([FromBody] NfcAdminRequest request)
        {
            var isEnroll = "enroll".Equals(request.Action, StringComparison.OrdinalIgnoreCase);
            var (isValid, errorResult, card) = await ValidateScanAsync(request, "/admin/card", skipCardLookup: isEnroll);
            if (!isValid) return errorResult!;

            if (isEnroll)
            {
                // Check if card already exists
                var existingCard = await _context.Cards.FirstOrDefaultAsync(c => c.Uid == request.Uid);
                if (existingCard != null)
                {
                    await LogAuditAsync(request.Uid, request.DeviceId, "/admin/card", 409, "denied");
                    return Conflict(new 
                    { 
                        allowed = false, 
                        message = _localizer.GetMessage("NFC_UID_ALREADY_ENROLLED_MSG"), 
                        sub = _localizer.GetMessage("NFC_UID_ALREADY_ENROLLED_SUB"), 
                        error = _localizer.GetMessage("NFC_UID_ALREADY_ENROLLED_ERR") 
                    });
                }

                // Create new active card
                var newCard = new Card
                {
                    Uid = request.Uid,
                    StudentId = null, // Can be linked to student record later via dashboard
                    Status = "active",
                    EnrolledBy = request.DeviceId,
                    EnrolledAt = DateTimeOffset.UtcNow
                };

                _context.Cards.Add(newCard);
                await LogAuditAsync(request.Uid, request.DeviceId, "/admin/card", 200, "granted");
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    allowed = true,
                    message = _localizer.GetMessage("NFC_CARD_ENROLLED_MSG"),
                    sub = _localizer.GetMessage("NFC_CARD_ENROLLED_SUB")
                });
            }
            else if ("deactivate".Equals(request.Action, StringComparison.OrdinalIgnoreCase))
            {
                // Card is retrieved during ValidateScanAsync
                card!.Status = "inactive";
                card.UpdatedAt = DateTimeOffset.UtcNow;

                await LogAuditAsync(request.Uid, request.DeviceId, "/admin/card", 200, "granted");
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    allowed = true,
                    message = _localizer.GetMessage("NFC_CARD_DEACTIVATED_MSG"),
                    sub = _localizer.GetMessage("NFC_CARD_DEACTIVATED_SUB")
                });
            }
            else if ("info".Equals(request.Action, StringComparison.OrdinalIgnoreCase))
            {
                // Card is retrieved during ValidateScanAsync
                var studentName = "Unlinked Student";
                if (card!.StudentId != null)
                {
                    var student = await _context.Students
                        .Include(s => s.User)
                        .FirstOrDefaultAsync(s => s.Id == card.StudentId);
                    studentName = student?.User?.Name ?? "Unlinked Student";
                }

                var statusText = card.Status == "active" ? "Active" : "Inactive";
                var subText = $"{studentName} - {statusText}";

                await LogAuditAsync(request.Uid, request.DeviceId, "/admin/card", 200, "granted");

                return Ok(new
                {
                    allowed = true,
                    message = _localizer.GetMessage("NFC_CARD_FOUND_MSG"),
                    sub = subText.Length > 21 ? subText.Substring(0, 18) + "..." : subText
                });
            }

            return BadRequest(new { error = _localizer.GetMessage("NFC_UNSUPPORTED_ACTION") });
        }

        [HttpPost("admin/card/link")]
        [RequirePermission(PermissionEnum.manage_cards)]
        public async Task<IActionResult> LinkCard([FromBody] NfcLinkRequest request)
        {
            // 1. Validate Secret
            var serverSecret = Environment.GetEnvironmentVariable("ESP32_SECRET");
            if (string.IsNullOrEmpty(serverSecret) || request.Secret != serverSecret)
            {
                return StatusCode(401, new { error = _localizer.GetMessage("NFC_INVALID_SECRET") });
            }

            // 2. Validate Card Uid Format
            if (string.IsNullOrEmpty(request.Uid) || !Regex.IsMatch(request.Uid, UidRegexPattern, RegexOptions.IgnoreCase))
            {
                return StatusCode(422, new { error = _localizer.GetMessage("NFC_INVALID_UID_FORMAT") });
            }

            // 3. Find Student
            var student = await _context.Students.FirstOrDefaultAsync(s => s.Id == request.StudentId);
            if (student == null)
            {
                return NotFound(new { error = _localizer.GetMessage("USER_NOT_FOUND") });
            }

            // 4. Find Card
            var card = await _context.Cards.FirstOrDefaultAsync(c => c.Uid == request.Uid);
            if (card == null)
            {
                return NotFound(new { error = _localizer.GetMessage("NFC_CARD_NOT_FOUND_ERR") });
            }

            // 5. Check if Student already has another card linked
            var studentCard = await _context.Cards.FirstOrDefaultAsync(c => c.StudentId == student.Id && c.Uid != card.Uid);
            if (studentCard != null)
            {
                return Conflict(new { error = "Student is already linked to another card." });
            }

            // 6. Link Card
            card.StudentId = student.Id;
            card.UpdatedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Card linked to student successfully.",
                uid = card.Uid,
                student_id = card.StudentId
            });
        }

        [HttpGet("attendance/test/diagnostics")]
        public async Task<IActionResult> GetDiagnostics()
        {
            var cards = await _context.Cards
                .Select(c => new { c.Uid, c.StudentId, c.Status })
                .ToListAsync();

            var studentCount = await _context.Students.CountAsync();
            var students = await _context.Students
                .Select(s => s.Id)
                .ToListAsync();

            var rooms = await _context.Rooms
                .Select(r => new { r.Id, r.Name, r.Code })
                .ToListAsync();

            return Ok(new { cards, studentCount, students, rooms });
        }
    }
}
