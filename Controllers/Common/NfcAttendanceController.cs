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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Threading.Channels;
using IbnElgm3a.Services.Localization;

namespace IbnElgm3a.Controllers.Common
{
    [ApiController]
    [Authorize]
    [BypassResponseWrapper]
    public class NfcAttendanceController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILocalizationService _localizer;
        private const string UidRegexPattern = @"^([0-9A-F]{2}:)*[0-9A-F]{2}$";

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, Channel<object>> _subscribers = new();

        private static void NotifyCardScanned(object cardDetails)
        {
            foreach (var channel in _subscribers.Values)
            {
                channel.Writer.TryWrite(cardDetails);
            }
        }

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

            // 4. Card Lookup - use Select projection instead of Include
            var cardData = await _context.Cards
                .AsNoTracking()
                .Where(c => c.Uid == request.Uid)
                .Select(c => new
                {
                    c.Id,
                    c.Uid,
                    c.Status,
                    c.StudentId,
                    StudentUserId = c.Student != null ? c.Student.UserId : null,
                    StudentName = c.Student != null && c.Student.User != null ? c.Student.User.Name : null
                })
                .FirstOrDefaultAsync();

            // Reconstruct a Card-like object for compatibility
            Card? card = null;
            if (cardData != null)
            {
                card = new Card
                {
                    Id = cardData.Id,
                    Uid = cardData.Uid,
                    Status = cardData.Status,
                    StudentId = cardData.StudentId
                };
                if (cardData.StudentId != null)
                {
                    card.Student = new IbnElgm3a.Models.Data.Student
                    {
                        Id = cardData.StudentId,
                        User = cardData.StudentName != null ? new IbnElgm3a.Models.Data.User { Name = cardData.StudentName } : null
                    };
                }
            }

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
        [AllowAnonymous]
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
                .AsNoTracking()
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
        [AllowAnonymous]
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
                .AsNoTracking()
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
        [AllowAnonymous]
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
                .AsNoTracking()
                .AnyAsync(cs => cs.StudentId == student.Id && cs.ExitTime == null && cs.EntryTime.Date == today);

            if (!hasOpenSession)
            {
                await LogAuditAsync(request.Uid, request.DeviceId, "/attendance/room", 403, "denied");
                return StatusCode(403, new { error = _localizer.GetMessage("NFC_NO_ENTRY_SESSION") });
            }

            // 2. Room lookup
            var roomIdStr = request.RoomId.ToString();
            var room = await _context.Rooms
                .AsNoTracking()
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
                .AsNoTracking()
                .Where(s => s.RoomId == room.Id && s.Day == dayEnum)
                .Select(s => new { s.StartTime, s.EndTime, s.SectionId })
                .ToListAsync();

            ScheduleSlot? activeSlot = null;
            string? activeSectionId = null;
            var currentLocalTime = localTime.TimeOfDay;
            foreach (var slot in slots)
            {
                if (TimeSpan.TryParse(slot.StartTime, out var slotStart) && TimeSpan.TryParse(slot.EndTime, out var slotEnd))
                {
                    var windowStart = slotStart.Add(TimeSpan.FromMinutes(-15));
                    var windowEnd = slotEnd.Add(TimeSpan.FromMinutes(15));
                    if (currentLocalTime >= windowStart && currentLocalTime <= windowEnd)
                    {
                        activeSectionId = slot.SectionId;
                        break;
                    }
                }
            }

            if (activeSectionId == null)
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

            // Get course name from schedule slot section - lightweight projection
            var courseName = await _context.Sections
                .AsNoTracking()
                .Where(sec => sec.Id == activeSectionId)
                .Select(sec => sec.Course != null ? sec.Course.Title : "Lecture")
                .FirstOrDefaultAsync() ?? "Lecture";

            // 4. Check for duplicate scan today
            var hasRegisteredToday = await _context.RoomAttendances
                .AsNoTracking()
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
        [AllowAnonymous]
        public async Task<IActionResult> AdminCardScan([FromBody] NfcAdminRequest request)
        {
            var isEnroll = "enroll".Equals(request.Action, StringComparison.OrdinalIgnoreCase);
            var (isValid, errorResult, card) = await ValidateScanAsync(request, "/admin/card", skipCardLookup: isEnroll);
            if (!isValid)
            {
                if (errorResult is NotFoundObjectResult)
                {
                    // Notify UI that a card was scanned but is pending registration
                    NotifyCardScanned(new
                    {
                        Id = (string?)null,
                        Uid = request.Uid,
                        Status = "pending",
                        StudentId = (string?)null,
                        StudentName = (string?)null,
                        DeviceId = request.DeviceId,
                        ScannedAt = DateTimeOffset.UtcNow
                    });
                }
                return errorResult!;
            }

            // Cache the last scanned card UID for the device and globally
            if (!string.IsNullOrEmpty(request.Uid))
            {
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                };
                _cache.Set($"last_scanned_card:{request.DeviceId}", request.Uid, cacheOptions);
                _cache.Set("last_scanned_card_global", request.Uid, cacheOptions);
            }

            if (isEnroll)
            {
                // Check if card already exists
                var existingCard = await _context.Cards.AsNoTracking().AnyAsync(c => c.Uid == request.Uid);
                if (existingCard)
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

                // Notify UI of the newly enrolled card
                NotifyCardScanned(new
                {
                    Id = newCard.Id,
                    Uid = newCard.Uid,
                    Status = newCard.Status,
                    StudentId = (string?)null,
                    StudentName = (string?)null,
                    DeviceId = request.DeviceId,
                    ScannedAt = DateTimeOffset.UtcNow
                });

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

                // Notify UI of deactivated card status
                NotifyCardScanned(new
                {
                    Id = card.Id,
                    Uid = card.Uid,
                    Status = card.Status,
                    StudentId = card.StudentId,
                    StudentName = card.Student != null && card.Student.User != null ? card.Student.User.Name : null,
                    DeviceId = request.DeviceId,
                    ScannedAt = DateTimeOffset.UtcNow
                });

                return Ok(new
                {
                    allowed = true,
                    message = _localizer.GetMessage("NFC_CARD_DEACTIVATED_MSG"),
                    sub = _localizer.GetMessage("NFC_CARD_DEACTIVATED_SUB")
                });
            }
            else if ("info".Equals(request.Action, StringComparison.OrdinalIgnoreCase))
            {
                // Get student name via lightweight projection
                string studentName = "Unlinked Student";
                if (card!.StudentId != null)
                {
                    studentName = await _context.Students
                        .AsNoTracking()
                        .Where(s => s.Id == card.StudentId)
                        .Select(s => s.User != null ? s.User.Name : "Unlinked Student")
                        .FirstOrDefaultAsync() ?? "Unlinked Student";
                }

                var statusText = card.Status == "active" ? "Active" : "Inactive";
                var subText = $"{studentName} - {statusText}";

                await LogAuditAsync(request.Uid, request.DeviceId, "/admin/card", 200, "granted");

                // Notify UI of card information scan
                NotifyCardScanned(new
                {
                    Id = card.Id,
                    Uid = card.Uid,
                    Status = card.Status,
                    StudentId = card.StudentId,
                    StudentName = studentName != "Unlinked Student" ? studentName : null,
                    DeviceId = request.DeviceId,
                    ScannedAt = DateTimeOffset.UtcNow
                });

                return Ok(new
                {
                    allowed = true,
                    message = _localizer.GetMessage("NFC_CARD_FOUND_MSG"),
                    sub = subText.Length > 21 ? subText.Substring(0, 18) + "..." : subText
                });
            }

            return BadRequest(new { error = _localizer.GetMessage("NFC_UNSUPPORTED_ACTION") });
        }

        [HttpGet("attendance/test/diagnostics")]
        [RequirePermission(PermissionEnum.manage_cards)]
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

        [HttpGet("admin/card/stream")]
        [RequirePermission(PermissionEnum.manage_cards)]
        public async Task GetCardStream(CancellationToken cancellationToken)
        {
            Response.ContentType = "text/event-stream";
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["Connection"] = "keep-alive";

            // Write initial connection success message
            await Response.WriteAsync("data: {\"status\":\"connected\"}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);

            var subscriptionId = Guid.NewGuid();
            var channel = Channel.CreateUnbounded<object>();
            _subscribers[subscriptionId] = channel;

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var readTask = channel.Reader.ReadAsync(cancellationToken).AsTask();
                    var delayTask = Task.Delay(5000, cancellationToken);

                    var completedTask = await Task.WhenAny(readTask, delayTask);
                    if (completedTask == readTask)
                    {
                        var cardDetails = await readTask;
                        var json = System.Text.Json.JsonSerializer.Serialize(cardDetails);
                        await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
                        await Response.Body.FlushAsync(cancellationToken);
                    }
                    else
                    {
                        // Heartbeat
                        await Response.WriteAsync(":\n\n", cancellationToken);
                        await Response.Body.FlushAsync(cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Disconnection expected
            }
            finally
            {
                _subscribers.TryRemove(subscriptionId, out _);
            }
        }

        [HttpPost("admin/card/link-student")]
        [RequirePermission(PermissionEnum.manage_cards)]
        public async Task<IActionResult> LinkCardForApp([FromBody] NfcLinkStudentRequest request)
        {
            if (string.IsNullOrEmpty(request.Uid) || !Regex.IsMatch(request.Uid, UidRegexPattern, RegexOptions.IgnoreCase))
            {
                return StatusCode(422, new { error = _localizer.GetMessage("NFC_INVALID_UID_FORMAT") });
            }

            var student = await _context.Students.FirstOrDefaultAsync(s => s.Id == request.StudentId || s.UserId == request.StudentId || s.AcademicNumber == request.StudentId);
            if (student == null)
            {
                return NotFound(new { error = _localizer.GetMessage("USER_NOT_FOUND") });
            }

            var card = await _context.Cards.FirstOrDefaultAsync(c => c.Uid == request.Uid);
            if (card == null)
            {
                card = new Card
                {
                    Uid = request.Uid,
                    StudentId = student.Id,
                    Status = "active",
                    EnrolledBy = "App",
                    EnrolledAt = DateTimeOffset.UtcNow
                };
                _context.Cards.Add(card);
            }
            else
            {
                var hasOtherCard = await _context.Cards.AsNoTracking().AnyAsync(c => c.StudentId == student.Id && c.Uid != card.Uid);
                if (hasOtherCard)
                {
                    return Conflict(new { error = _localizer.GetMessage("NFC_STUDENT_ALREADY_LINKED") });
                }

                card.StudentId = student.Id;
                card.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = _localizer.GetMessage("NFC_CARD_LINKED_SUCCESS"),
                uid = card.Uid,
                student_id = card.StudentId
            });
        }

        [HttpPatch("admin/card/link-student")]
        [RequirePermission(PermissionEnum.manage_cards)]
        public async Task<IActionResult> UpdateCardLinkForApp([FromBody] NfcLinkStudentRequest request)
        {
            if (string.IsNullOrEmpty(request.Uid) || !Regex.IsMatch(request.Uid, UidRegexPattern, RegexOptions.IgnoreCase))
            {
                return StatusCode(422, new { error = _localizer.GetMessage("NFC_INVALID_UID_FORMAT") });
            }

            var card = await _context.Cards.FirstOrDefaultAsync(c => c.Uid == request.Uid);
            if (card == null)
            {
                return NotFound(new { error = _localizer.GetMessage("NFC_CARD_NOT_FOUND_ERR") });
            }

            var student = await _context.Students.FirstOrDefaultAsync(s => s.Id == request.StudentId || s.UserId == request.StudentId || s.AcademicNumber == request.StudentId);
            if (student == null)
            {
                return NotFound(new { error = _localizer.GetMessage("USER_NOT_FOUND") });
            }

            var hasOtherCard = await _context.Cards.AsNoTracking().AnyAsync(c => c.StudentId == student.Id && c.Uid != card.Uid);
            if (hasOtherCard)
            {
                return Conflict(new { error = _localizer.GetMessage("NFC_STUDENT_ALREADY_LINKED") });
            }

            card.StudentId = student.Id;
            card.UpdatedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = _localizer.GetMessage("NFC_CARD_LINKED_SUCCESS"),
                uid = card.Uid,
                student_id = card.StudentId
            });
        }

        [HttpDelete("admin/card/link-student")]
        [RequirePermission(PermissionEnum.manage_cards)]
        public async Task<IActionResult> UnlinkCardForApp([FromQuery] string? uid = null, [FromQuery] string? studentId = null)
        {
            if (string.IsNullOrEmpty(uid) && string.IsNullOrEmpty(studentId))
            {
                return BadRequest(new { error = "Either Uid or studentId must be provided." });
            }

            Card? card = null;
            if (!string.IsNullOrEmpty(uid))
            {
                if (!Regex.IsMatch(uid, UidRegexPattern, RegexOptions.IgnoreCase))
                {
                    return StatusCode(422, new { error = _localizer.GetMessage("NFC_INVALID_UID_FORMAT") });
                }
                card = await _context.Cards.FirstOrDefaultAsync(c => c.Uid == uid);
            }
            else if (!string.IsNullOrEmpty(studentId))
            {
                card = await _context.Cards.FirstOrDefaultAsync(c => c.StudentId == studentId);
            }

            if (card == null)
            {
                return NotFound(new { error = _localizer.GetMessage("NFC_CARD_NOT_FOUND_ERR") });
            }

            card.StudentId = null;
            card.UpdatedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = _localizer.GetMessage("NFC_CARD_UNLINKED_SUCCESS"),
                uid = card.Uid
            });
        }

        // [HttpGet("admin/card/last-scanned")]
        // public async Task<IActionResult> GetLastScannedCard([FromQuery] string? deviceId = null)
        // {
        //     var cacheKey = !string.IsNullOrEmpty(deviceId) 
        //         ? $"last_scanned_card:{deviceId}" 
        //         : "last_scanned_card_global";

        //     if (_cache.TryGetValue(cacheKey, out string? uid) && !string.IsNullOrEmpty(uid))
        //     {
        //         var card = await _context.Cards
        //             .AsNoTracking()
        //             .Where(c => c.Uid == uid)
        //             .Select(c => new
        //             {
        //                 c.Id,
        //                 c.Uid,
        //                 c.Status,
        //                 c.StudentId,
        //                 StudentName = c.Student != null && c.Student.User != null ? c.Student.User.Name : null
        //             })
        //             .FirstOrDefaultAsync();

        //         if (card != null)
        //         {
        //             return Ok(card);
        //         }

        //         return Ok(new
        //         {
        //             Uid = uid,
        //             Status = "pending",
        //             StudentId = (string?)null,
        //             StudentName = (string?)null
        //         });
        //     }

        //     return NotFound(new { error = _localizer.GetMessage("NFC_NO_CARD_RECENTLY_SCANNED") });
        // }

        // [HttpGet("admin/card/by-uid/{uid}")]
        // public async Task<IActionResult> GetCardByUid(string uid)
        // {
        //     if (string.IsNullOrEmpty(uid) || !Regex.IsMatch(uid, UidRegexPattern, RegexOptions.IgnoreCase))
        //     {
        //         return StatusCode(422, new { error = _localizer.GetMessage("NFC_INVALID_UID_FORMAT") });
        //     }

        //     var card = await _context.Cards
        //         .AsNoTracking()
        //         .Where(c => c.Uid == uid)
        //         .Select(c => new
        //         {
        //             c.Id,
        //             c.Uid,
        //             c.Status,
        //             c.StudentId,
        //             StudentName = c.Student != null && c.Student.User != null ? c.Student.User.Name : null
        //         })
        //         .FirstOrDefaultAsync();

        //     if (card == null)
        //     {
        //         return NotFound(new { error = _localizer.GetMessage("NFC_CARD_NOT_FOUND_ERR") });
        //     }

        //     return Ok(card);
        // }
    }
}
