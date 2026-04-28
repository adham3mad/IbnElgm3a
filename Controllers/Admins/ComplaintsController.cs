using IbnElgm3a.DTOs.Complaints;
using IbnElgm3a.DTOs.Common;
using IbnElgm3a.Enums;
using IbnElgm3a.Models;
using IbnElgm3a.Filters;
using IbnElgm3a.Services.Localization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IbnElgm3a.Models;
using IbnElgm3a.Models.Data;
using Microsoft.EntityFrameworkCore;

namespace IbnElgm3a.Controllers.Admins
{
    [ApiController]
    [Route("v1/admin/complaints")]
    [Authorize]
    public class ComplaintsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILocalizationService _localizer;

        public ComplaintsController(AppDbContext context, ILocalizationService localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        [HttpGet]
        [RequirePermission(PermissionEnum.Dashboard_Complaints_Read)]
        public async Task<IActionResult> GetComplaints(
            [FromQuery] ComplaintStatus? status = null,
            [FromQuery] ComplaintType? type = null,
            [FromQuery] bool? urgent = null,
            [FromQuery] string? q = null,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 20,
            [FromQuery] string sort_by = "created_at",
            [FromQuery] string sort_dir = "desc")
        {
            var query = _context.Complaints
                .Include(c => c.Student)
                .Include(c => c.AssignedTo)
                .AsQueryable();

            if (status.HasValue) query = query.Where(c => c.Status == status.Value);
            if (type.HasValue) query = query.Where(c => c.Type == type.Value);
            
            if (urgent.HasValue && urgent.Value)
            {
                // Simple logic: complaints older than 48 hours are urgent if not resolved
                var threshold = System.DateTimeOffset.UtcNow.AddHours(-48);
                query = query.Where(c => c.CreatedAt < threshold && c.Status != ComplaintStatus.Resolved);
            }

            if (!string.IsNullOrEmpty(q))
            {
                var qLower = q.ToLower();
                query = query.Where(c => c.Title.ToLower().Contains(qLower) || c.TicketNumber.ToLower().Contains(qLower));
            }

            var total = await query.CountAsync();

            query = sort_by switch
            {
                "created_at" => sort_dir == "asc" ? query.OrderBy(c => c.CreatedAt) : query.OrderByDescending(c => c.CreatedAt),
                "title" => sort_dir == "asc" ? query.OrderBy(c => c.Title) : query.OrderByDescending(c => c.Title),
                _ => query.OrderByDescending(c => c.CreatedAt)
            };

            var complaints = await query
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(c => new ComplaintListResponseDto
                {
                    Id = c.Id,
                    TicketNumber = c.TicketNumber,
                    Student = c.Student != null ? new StudentSummaryDto { Id = c.StudentId, Name = c.Student.Name, StudentId = c.Student.NationalId } : null,
                    Type = c.Type,
                    Title = c.Title,
                    Status = c.Status,
                    IsOverdue = (System.DateTimeOffset.UtcNow - c.CreatedAt).TotalHours > 72,
                    AssignedTo = c.AssignedTo != null ? new IdNameDto { Id = c.AssignedToId ?? "", Name = c.AssignedTo.Name } : null,
                    CreatedAt = c.CreatedAt,
                    LastResponseAt = c.LastResponseAt
                }).ToListAsync();

            var pag = new ApiPagination { Page = page, Limit = limit, Total = total, HasMore = (page * limit) < total };
            return Ok(ApiResponse<List<ComplaintListResponseDto>>.CreateSuccess(complaints, pagination: pag));
        }

        [HttpGet("{complaint_id}")]
        [RequirePermission(PermissionEnum.Dashboard_Complaints_Read)]
        public async Task<IActionResult> GetComplaintById(string complaint_id)
        {
            var c = await _context.Complaints
                .Include(comp => comp.Student)
                .Include(comp => comp.AssignedTo)
                .Include(comp => comp.InternalNotes)
                    .ThenInclude(n => n.Author)
                .FirstOrDefaultAsync(comp => comp.Id == complaint_id);

            if (c == null) return NotFound(ApiResponse<object>.CreateError("COMPLAINT_NOT_FOUND", _localizer.GetMessage("COMPLAINT_NOT_FOUND")));

            var dto = new ComplaintDetailResponseDto
            {
                Id = c.Id,
                TicketNumber = c.TicketNumber,
                Student = c.Student != null ? new StudentSummaryDto { Id = c.StudentId, Name = c.Student.Name, StudentId = c.Student.NationalId } : null,
                Type = c.Type,
                Title = c.Title,
                Status = c.Status,
                IsOverdue = (System.DateTimeOffset.UtcNow - c.CreatedAt).TotalHours > 72,
                AssignedTo = c.AssignedTo != null ? new IdNameDto { Id = c.AssignedToId ?? "", Name = c.AssignedTo.Name } : null,
                CreatedAt = c.CreatedAt,
                LastResponseAt = c.LastResponseAt,
                Description = c.Description,
                Response = c.Response,
                InternalNotes = c.InternalNotes.Select(n => new InternalNoteDto
                {
                    Id = n.Id,
                    AuthorName = n.Author?.Name ?? "Admin",
                    Text = n.Text,
                    CreatedAt = n.CreatedAt
                }).ToList()
            };

            return Ok(ApiResponse<ComplaintDetailResponseDto>.CreateSuccess(dto));
        }

        [HttpPatch("{complaint_id}")]
        [RequirePermission(PermissionEnum.Dashboard_Complaints_Update)]
        public async Task<IActionResult> UpdateComplaint(string complaint_id, [FromBody] UpdateComplaintRequestDto request)
        {
            var complaint = await _context.Complaints.FindAsync(complaint_id);
            if (complaint == null) return NotFound(ApiResponse<object>.CreateError("COMPLAINT_NOT_FOUND", _localizer.GetMessage("COMPLAINT_NOT_FOUND")));

            if (request.Status.HasValue) complaint.Status = request.Status.Value;
            if (request.AssignedTo != null) complaint.AssignedToId = request.AssignedTo;
            if (request.Response != null)
            {
                complaint.Response = request.Response;
                complaint.LastResponseAt = System.DateTimeOffset.UtcNow;
            }
            
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.CreateSuccess(new { message = _localizer.GetMessage("COMPLAINT_UPDATED") }));
        }

        [HttpDelete("{complaint_id}")]
        [RequirePermission(PermissionEnum.Dashboard_Complaints_Delete)]
        public async Task<IActionResult> DeleteComplaint(string complaint_id)
        {
            var complaint = await _context.Complaints.FindAsync(complaint_id);
            if (complaint == null) return NotFound(ApiResponse<object>.CreateError("COMPLAINT_NOT_FOUND", _localizer.GetMessage("COMPLAINT_NOT_FOUND")));

            _context.Complaints.Remove(complaint);
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.CreateSuccess(new { message = _localizer.GetMessage("DELETED_SUCCESS") }));
        }

        [HttpPost("{complaint_id}/internal-notes")]
        [RequirePermission(PermissionEnum.Dashboard_Complaints_Update)]
        public async Task<IActionResult> AddInternalNote(string complaint_id, [FromBody] System.Text.Json.JsonElement request)
        {
            var complaint = await _context.Complaints.FindAsync(complaint_id);
            if (complaint == null) return NotFound(ApiResponse<object>.CreateError("COMPLAINT_NOT_FOUND", _localizer.GetMessage("COMPLAINT_NOT_FOUND")));

            var text = request.GetProperty("text").GetString();
            if (string.IsNullOrEmpty(text)) return BadRequest(ApiResponse<object>.CreateError("EMPTY_NOTE", "Note text is required"));

            var note = new ComplaintNote
            {
                ComplaintId = complaint_id,
                AuthorId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "",
                Text = text
            };

            _context.ComplaintNotes.Add(note);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.CreateSuccess(new { message = "Note added successfully" }));
        }
    }
}
