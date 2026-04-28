using IbnElgm3a.Models;
using IbnElgm3a.Models.Data;
using IbnElgm3a.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace IbnElgm3a.Controllers.Students
{
    [ApiController]
    [Route("student/complaints")]
    [Authorize]
    public class StudentComplaintsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StudentComplaintsController(AppDbContext context)
        {
            _context = context;
        }

        private string GetUserId() => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";

        [HttpGet]
        public async Task<IActionResult> GetComplaints([FromQuery] string? status = null, [FromQuery] int page = 1, [FromQuery] int per_page = 20)
        {
            var userId = GetUserId();
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            if (student == null) return Unauthorized();

            var query = _context.Complaints
                .Where(c => c.StudentId == student.Id)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<ComplaintStatus>(status, true, out var parsedStatus))
            {
                query = query.Where(c => c.Status == parsedStatus);
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
                    has_unread_reply = false, // mock
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
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            if (student == null) return Unauthorized();

            var complaint = await _context.Complaints
                .FirstOrDefaultAsync(c => c.Id == id && c.StudentId == student.Id);

            if (complaint == null) return NotFound(new { error = "not_found", message = "Complaint not found" });

            var messages = await _context.ComplaintMessages
                .Include(m => m.Sender)
                .Where(m => m.ComplaintId == complaint.Id)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            var thread = new List<object>();

            // Add original description as first message for uniform thread UX
            if (!string.IsNullOrEmpty(complaint.Description)) 
            {
                thread.Add(new
                {
                    id = "msg_" + complaint.Id,
                    sender_role = "student",
                    sender_name = student.User?.Name ?? "Student",
                    message = complaint.Description,
                    sent_at = complaint.CreatedAt,
                    attachments = new List<object>() // attachments handle omitted for simplicity
                });
            }

            thread.AddRange(messages.Select(m => new
            {
                id = m.Id,
                sender_role = m.SenderRole,
                sender_name = m.Sender?.Name ?? "Admin",
                message = m.Message,
                sent_at = m.CreatedAt,
                attachments = new List<object>() // parsed from m.AttachmentsJson normally
            }));

            var result = new
            {
                id = complaint.Id,
                @ref = "#" + complaint.TicketNumber,
                category = complaint.Type.ToString().ToLower(),
                title = complaint.Title,
                status = complaint.Status.ToString().ToLower(),
                created_at = complaint.CreatedAt,
                attachments = new List<object>(), // mock
                thread = thread
            };

            return Ok(result);
        }

        public class SubmitComplaintDto
        {
            public string Category { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
        }

        [HttpPost]
        public async Task<IActionResult> SubmitComplaint([FromForm] SubmitComplaintDto dto)
        {
            // Note: IFormFile attachments processing omitted for brevity
            var userId = GetUserId();
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            if (student == null) return Unauthorized();

            var type = Enum.TryParse<ComplaintType>(dto.Category, true, out var t) ? t : ComplaintType.Other;

            var comp = new Complaint
            {
                StudentId = student.Id,
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
                message = "Complaint submitted successfully."
            });
        }

        public class ReplyComplaintDto
        {
            public string Message { get; set; } = string.Empty;
        }

        [HttpPost("{id}/reply")]
        public async Task<IActionResult> ReplyComplaint(string id, [FromForm] ReplyComplaintDto dto)
        {
            var userId = GetUserId();
            var student = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.UserId == userId);
            if (student == null) return Unauthorized();

            var complaint = await _context.Complaints.FirstOrDefaultAsync(c => c.Id == id && c.StudentId == student.Id);
            if (complaint == null) return NotFound(new { error = "not_found", message = "Complaint not found" });

            if (complaint.Status == ComplaintStatus.Closed)
                return StatusCode(410, new { error = "gone", message = "Complaint is closed" });

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
                sender_name = student.User?.Name,
                message = msg.Message,
                sent_at = msg.CreatedAt,
                attachments = new List<object>()
            });
        }
    }
}
