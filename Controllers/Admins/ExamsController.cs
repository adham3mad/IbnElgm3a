using IbnElgm3a.Models;
using IbnElgm3a.DTOs.Exams;
using IbnElgm3a.DTOs.Courses;
using IbnElgm3a.DTOs.Rooms;
using IbnElgm3a.Filters;
using IbnElgm3a.Enums;
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
    [Route("v1/admin/exams")]
    [Authorize]
    public class ExamsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILocalizationService _localizer;

        public ExamsController(AppDbContext context, ILocalizationService localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        [HttpGet]
        [RequirePermission(PermissionEnum.Dashboard_Exams_Read)]
        public async Task<IActionResult> GetExams(
            [FromQuery] string? semester_id = null,
            [FromQuery] string? type = null,
            [FromQuery] string? status = null,
            [FromQuery] string? date = null)
        {
            var query = _context.Exams
                .Include(e => e.Course)
                .Include(e => e.Hall)
                .AsQueryable();

            if (!string.IsNullOrEmpty(semester_id)) query = query.Where(e => e.SemesterId == semester_id);
            if (!string.IsNullOrEmpty(date) && System.DateTimeOffset.TryParse(date, out var parsedDate))
            {
                query = query.Where(e => e.Date.Date == parsedDate.Date);
            }
            if (!string.IsNullOrEmpty(status) && System.Enum.TryParse<IbnElgm3a.Enums.ExamStatus>(status, true, out var parsedStatus))
                query = query.Where(e => e.Status == parsedStatus);
                
            if (!string.IsNullOrEmpty(type) && System.Enum.TryParse<IbnElgm3a.Enums.ExamType>(type, true, out var parsedType))
                query = query.Where(e => e.Type == parsedType);

            var exams = await query
                .OrderByDescending(e => e.Date)
                .Select(e => new ExamListResponseDto
                {
                    Id = e.Id,
                    Course = e.Course != null ? new CourseSummaryDto { Id = e.Course.Id, Code = e.Course.CourseCode, Name = e.Course.Title } : null,
                    Type = e.Type,
                    Date = e.Date,
                    StartTime = e.StartTime,
                    DurationMinutes = e.DurationMinutes,
                    Hall = e.Hall != null ? new RoomResponseDto { Id = e.Hall.Id, Name = e.Hall.Name, Capacity = e.Hall.Capacity } : null,
                    EnrolledCount = e.EnrolledCount,
                    Status = e.Status,
                    HasSeatPlan = e.HasSeatPlan,
                    SeatingStrategy = e.SeatingStrategy,
                    SeatPlanPdfUrl = e.SeatPlanPdfUrl,
                    PublishedAt = e.PublishedAt
                }).ToListAsync();

            return Ok(ApiResponse<List<ExamListResponseDto>>.CreateSuccess(exams));
        }

        [HttpPost]
        [RequirePermission(PermissionEnum.Dashboard_Exams_Create)]
        public async Task<IActionResult> CreateExam([FromBody] CreateExamRequestDto request)
        {
            if (!string.IsNullOrEmpty(request.CourseId))
            {
                var courseExists = await _context.Courses.AnyAsync(c => c.Id == request.CourseId);
                if (!courseExists) return BadRequest(ApiResponse<object>.CreateError("COURSE_NOT_FOUND", _localizer.GetMessage("COURSE_NOT_FOUND")));
            }

            if (!string.IsNullOrEmpty(request.HallId))
            {
                var hallExists = await _context.Rooms.AnyAsync(r => r.Id == request.HallId);
                if (!hallExists) return BadRequest(ApiResponse<object>.CreateError("HALL_NOT_FOUND", _localizer.GetMessage("HALL_NOT_FOUND")));
            }

            if (!string.IsNullOrEmpty(request.SemesterId))
            {
                var semExists = await _context.Semesters.AnyAsync(s => s.Id == request.SemesterId);
                if (!semExists) return BadRequest(ApiResponse<object>.CreateError("SEMESTER_NOT_FOUND", _localizer.GetMessage("SEMESTER_NOT_FOUND")));
            }

            var exam = new Exam
            {
                Id = "exam_" + System.Guid.NewGuid().ToString("N").Substring(0, 10),
                CourseId = request.CourseId,
                SemesterId = request.SemesterId,
                HallId = request.HallId,
                Date = request.Date,
                StartTime = request.StartTime,
                DurationMinutes = request.DurationMinutes,
                Type = request.Type,
                Status = IbnElgm3a.Enums.ExamStatus.Draft
            };

            _context.Exams.Add(exam);
            await _context.SaveChangesAsync();

            return Created("", ApiResponse<object>.CreateSuccess(new { id = exam.Id }));
        }

        [HttpPost("{exam_id}/publish")]
        [RequirePermission(PermissionEnum.Dashboard_Exams_Update)]
        public async Task<IActionResult> PublishExam(string exam_id)
        {
            var exam = await _context.Exams.FindAsync(exam_id);
            if (exam == null) return NotFound(ApiResponse<object>.CreateError("EXAM_NOT_FOUND", _localizer.GetMessage("EXAM_NOT_FOUND")));

            exam.Status = IbnElgm3a.Enums.ExamStatus.Published;
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.CreateSuccess(new { message = _localizer.GetMessage("EXAM_PUBLISHED"), published_at = System.DateTimeOffset.UtcNow }));
        }

        [HttpPost("{exam_id}/seat-assignments")]
        [RequirePermission(PermissionEnum.Dashboard_Exams_Update)]
        public async Task<IActionResult> GenerateSeatAssignments(string exam_id, [FromBody] GenerateSeatAssignmentsRequestDto request)
        {
            var exam = await _context.Exams.Include(e => e.Hall).FirstOrDefaultAsync(e => e.Id == exam_id);
            if (exam == null) return NotFound(ApiResponse<object>.CreateError("EXAM_NOT_FOUND", _localizer.GetMessage("EXAM_NOT_FOUND")));

            // Use strategy from request or model
            SeatingStrategy strategy = request.Strategy ?? exam.SeatingStrategy;
            exam.SeatingStrategy = strategy;
            exam.HasSeatPlan = true;
            exam.SeatPlanPdfUrl = $"https://cdn.masaar.edu.eg/layouts/{exam_id}.pdf";
            
            await _context.SaveChangesAsync();

            var resp = new
            {
                assigned_count = exam.EnrolledCount > 0 ? exam.EnrolledCount : 50,
                hall_id = exam.HallId,
                layout_url = exam.SeatPlanPdfUrl,
                strategy = strategy.ToString()
            };

            return Ok(ApiResponse<object>.CreateSuccess(resp));
        }

        [HttpGet("{exam_id}")]
        [RequirePermission(PermissionEnum.Dashboard_Exams_Read)]
        public async Task<IActionResult> GetExamById(string exam_id)
        {
            var exam = await _context.Exams
                .Include(e => e.Course)
                .Include(e => e.Hall)
                .FirstOrDefaultAsync(e => e.Id == exam_id);

            if (exam == null) return NotFound(ApiResponse<object>.CreateError("EXAM_NOT_FOUND", _localizer.GetMessage("EXAM_NOT_FOUND")));

            var dto = new ExamListResponseDto
            {
                Id = exam.Id,
                Course = exam.Course != null ? new CourseSummaryDto { Id = exam.Course.Id, Code = exam.Course.CourseCode, Name = exam.Course.Title } : null,
                Type = exam.Type,
                Date = exam.Date,
                StartTime = exam.StartTime,
                DurationMinutes = exam.DurationMinutes,
                Hall = exam.Hall != null ? new RoomResponseDto { Id = exam.Hall.Id, Name = exam.Hall.Name, Capacity = exam.Hall.Capacity } : null,
                EnrolledCount = exam.EnrolledCount,
                Status = exam.Status,
                HasSeatPlan = exam.HasSeatPlan,
                SeatingStrategy = exam.SeatingStrategy,
                SeatPlanPdfUrl = exam.SeatPlanPdfUrl,
                PublishedAt = exam.PublishedAt
            };

            return Ok(ApiResponse<ExamListResponseDto>.CreateSuccess(dto));
        }

        [HttpPatch("{exam_id}")]
        [RequirePermission(PermissionEnum.Dashboard_Exams_Update)]
        public async Task<IActionResult> UpdateExam(string exam_id, [FromBody] UpdateExamRequestDto request)
        {
            var exam = await _context.Exams.FindAsync(exam_id);
            if (exam == null) return NotFound(ApiResponse<object>.CreateError("EXAM_NOT_FOUND", _localizer.GetMessage("EXAM_NOT_FOUND")));

            if (!string.IsNullOrEmpty(request.CourseId)) exam.CourseId = request.CourseId;
            if (!string.IsNullOrEmpty(request.SemesterId)) exam.SemesterId = request.SemesterId;
            if (request.Type.HasValue) exam.Type = request.Type.Value;
            if (request.Date.HasValue) exam.Date = request.Date.Value;
            if (!string.IsNullOrEmpty(request.StartTime)) exam.StartTime = request.StartTime;
            if (request.DurationMinutes.HasValue) exam.DurationMinutes = request.DurationMinutes.Value;
            if (!string.IsNullOrEmpty(request.HallId)) exam.HallId = request.HallId;
            if (request.Status.HasValue) exam.Status = request.Status.Value;
            if (request.SeatingStrategy.HasValue) exam.SeatingStrategy = request.SeatingStrategy.Value;

            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.CreateSuccess(new { message = _localizer.GetMessage("UPDATED_SUCCESS") }));
        }

        [HttpDelete("{exam_id}")]
        [RequirePermission(PermissionEnum.Dashboard_Exams_Delete)]
        public async Task<IActionResult> DeleteExam(string exam_id)
        {
            var exam = await _context.Exams.FindAsync(exam_id);
            if (exam == null) return NotFound(ApiResponse<object>.CreateError("EXAM_NOT_FOUND", _localizer.GetMessage("EXAM_NOT_FOUND")));

            _context.Exams.Remove(exam);
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.CreateSuccess(new { message = _localizer.GetMessage("DELETED_SUCCESS") }));
        }
    }
}
