using IbnElgm3a.Models;
using IbnElgm3a.Models.Data;
using IbnElgm3a.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IbnElgm3a.DTOs.Complaints;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using IbnElgm3a.Services.Localization;

namespace IbnElgm3a.Controllers.Students
{
    [ApiController]
    [Route("student/complaints")]
    [Authorize(Roles = "student")]
    public class StudentComplaintsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILocalizationService _localizer;

        public StudentComplaintsController(AppDbContext context, ILocalizationService localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        private string GetUserId() => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";

        [HttpGet]
        public async Task<IActionResult> GetComplaints([FromQuery] ComplaintStatus? status = null, [FromQuery] int page = 1, [FromQuery] int per_page = 20)
        {
            var userId = GetUserId();

            // Single query: fetch student ID and lastActiveAt together
            var userData = await _context.Students
                .AsNoTracking()
                .Where(s => s.UserId == userId)
                .Select(s => new
                {
                    s.Id,
                    LastActiveAt = s.User != null ? s.User.LastActiveAt : (DateTimeOffset?)null
                })
                .FirstOrDefaultAsync();

            if (userData == null) return Unauthorized();

            var lastActiveAt = userData.LastActiveAt ?? DateTimeOffset.MinValue;

            var query = _context.Complaints
                .AsNoTracking()
                .Where(c => c.StudentId == userId);

            if (status.HasValue)
            {
                query = query.Where(c => c.Status == status.Value);
            }

            var total = await query.CountAsync();
            var complaints = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * per_page)
                .Take(per_page)
                .Select(c => new
                {
                    id = c.Id,
                    @ref = "#" + c.TicketNumber,
                    category = c.Type.ToString().ToLower(),
                    title = c.Title,
                    description = c.Description,
                    status = c.Status.ToString().ToLower(),
                    has_unread_reply = c.LastResponseAt > lastActiveAt,
                    reply_count = _context.ComplaintMessages.Count(m => m.ComplaintId == c.Id),
                    last_reply_at = c.LastResponseAt,
                    created_at = c.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                total = total,
                page = page,
                per_page = per_page,
                complaints = complaints
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetComplaint(string id)
        {
            var userId = GetUserId();

            var studentData = await _context.Students
                .AsNoTracking()
                .Where(s => s.UserId == userId)
                .Select(s => new { s.Id, UserName = s.User != null ? s.User.Name : null })
                .FirstOrDefaultAsync();

            if (studentData == null) return Unauthorized();

            var complaint = await _context.Complaints
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id && c.StudentId == userId);

            if (complaint == null) return NotFound(new { error = "not_found", message = _localizer.GetMessage("COMPLAINT_NOT_FOUND") });

            var messages = await _context.ComplaintMessages
                .AsNoTracking()
                .Where(m => m.ComplaintId == complaint.Id)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new
                {
                    m.Id,
                    m.SenderRole,
                    SenderName = m.Sender != null ? m.Sender.Name : "Admin",
                    m.Message,
                    m.CreatedAt
                })
                .ToListAsync();

            var thread = new List<object>();

            if (!string.IsNullOrEmpty(complaint.Description))
            {
                thread.Add(new
                {
                    id = "msg_" + complaint.Id,
                    sender_role = "student",
                    sender_name = studentData.UserName ?? "Student",
                    message = complaint.Description,
                    sent_at = complaint.CreatedAt,
                    attachments = new List<object>()
                });
            }

            thread.AddRange(messages.Select(m => (object)new
            {
                id = m.Id,
                sender_role = m.SenderRole,
                sender_name = m.SenderName,
                message = m.Message,
                sent_at = m.CreatedAt,
                attachments = new List<object>()
            }));

            var result = new
            {
                id = complaint.Id,
                @ref = "#" + complaint.TicketNumber,
                category = complaint.Type.ToString().ToLower(),
                title = complaint.Title,
                status = complaint.Status.ToString().ToLower(),
                created_at = complaint.CreatedAt,
                attachments = new List<object>(),
                thread = thread
            };

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> SubmitComplaint([FromForm] SubmitComplaintDto dto)
        {
            var userId = GetUserId();
            var studentId = await _context.Students
                .AsNoTracking()
                .Where(s => s.UserId == userId)
                .Select(s => s.Id)
                .FirstOrDefaultAsync();

            if (studentId == null) return Unauthorized();

            var type = Enum.TryParse<ComplaintType>(dto.Category, true, out var t) ? t : ComplaintType.Other;

            var comp = new Complaint
            {
                StudentId = userId,
                Type = type,
                Title = dto.Title,
                Description = dto.Description,
                TicketNumber = "C-" + new Random().Next(1000, 9999).ToString(),
                Status = ComplaintStatus.Open
            };

            _context.Complaints.Add(comp);
            await _context.SaveChangesAsync();

            return Created($"/student/complaints/{comp.Id}", new
            {
                id = comp.Id,
                @ref = "#" + comp.TicketNumber,
                status = "open",
                message = _localizer.GetMessage("COMPLAINT_SUBMITTED")
            });
        }

        [HttpPost("{id}/reply")]
        public async Task<IActionResult> ReplyComplaint(string id, [FromForm] ReplyComplaintDto dto)
        {
            var userId = GetUserId();

            var studentData = await _context.Students
                .AsNoTracking()
                .Where(s => s.UserId == userId)
                .Select(s => new { s.Id, UserName = s.User != null ? s.User.Name : null })
                .FirstOrDefaultAsync();

            if (studentData == null) return Unauthorized();

            var complaint = await _context.Complaints.FirstOrDefaultAsync(c => c.Id == id && c.StudentId == userId);
            if (complaint == null) return NotFound(new { error = "not_found", message = "Complaint not found" });

            if (complaint.Status == ComplaintStatus.Closed)
                return StatusCode(410, new { error = "gone", message = _localizer.GetMessage("COMPLAINT_CLOSED") });

            var msg = new ComplaintMessage
            {
                ComplaintId = complaint.Id,
                SenderId = userId,
                SenderRole = "student",
                Message = dto.Message
            };

            _context.ComplaintMessages.Add(msg);

            complaint.LastResponseAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                id = msg.Id,
                sender_role = "student",
                sender_name = studentData.UserName,
                message = msg.Message,
                sent_at = msg.CreatedAt,
                attachments = new List<object>()
            });
        }
    }
}
