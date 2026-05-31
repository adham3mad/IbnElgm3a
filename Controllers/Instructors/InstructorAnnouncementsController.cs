using IbnElgm3a.Models;
using IbnElgm3a.Models.Data;
using IbnElgm3a.Services;
using IbnElgm3a.Services.Localization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using IbnElgm3a.DTOs.Announcements;

namespace IbnElgm3a.Controllers.Instructors
{
    [ApiController]
    [Route("instructor/announcements")]
    [Authorize(Roles = "instructor")]
    public class InstructorAnnouncementsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILocalizationService _localizer;

        public InstructorAnnouncementsController(AppDbContext context, ILocalizationService localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        [HttpGet]
        public async Task<IActionResult> GetAnnouncements([FromQuery] string? course_id)
        {
            var userId = GetUserId();
            var query = _context.Announcements
                .Include(a => a.AnnouncementCourses)
                .Where(a => a.CreatedById == userId);

            if (!string.IsNullOrEmpty(course_id))
            {
                query = query.Where(a => a.AnnouncementCourses.Any(ac => ac.CourseId == course_id));
            }

            var rawAnnouncements = await query
                .Select(a => new
                {
                    id = a.Id,
                    title = a.Title,
                    body = a.Body,
                    course_ids = a.AnnouncementCourses.Select(ac => ac.CourseId).ToList(),
                    audience = a.Audience,
                    send_push = a.SendPush,
                    has_attachment = !string.IsNullOrEmpty(a.AttachmentUrl),
                    status = a.Status,
                    scheduled_at = a.ScheduledAt,
                    published_at = a.Status == "published" ? a.CreatedAt : (DateTimeOffset?)null,
                    notified_students_count = a.SentCount,
                    created_at = a.CreatedAt
                })
                .ToListAsync();

            // Order by published_at DESC (nulls — drafts and scheduled — sorted last by created_at DESC)
            var announcements = rawAnnouncements
                .OrderByDescending(a => a.published_at.HasValue)
                .ThenByDescending(a => a.published_at)
                .ThenByDescending(a => a.created_at)
                .Select(a => new
                {
                    a.id,
                    a.title,
                    a.body,
                    a.course_ids,
                    a.audience,
                    a.send_push,
                    a.has_attachment,
                    a.status,
                    a.scheduled_at,
                    a.published_at,
                    a.notified_students_count
                })
                .ToList();

            return Ok(new { announcements = announcements });
        }

        [HttpPost]
        public async Task<IActionResult> CreateAnnouncement([FromBody] AnnouncementRequest request)
        {
            var userId = GetUserId();
            var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.UserId == userId);
            if (instructor == null) return Unauthorized();

            if (request.CourseIds == null || !request.CourseIds.Any())
            {
                return BadRequest(ApiResponse<object>.CreateError("COURSE_IDS_REQUIRED", "At least one course ID must be specified."));
            }

            // Validate that all course IDs belong to this instructor
            var instructorCourseIds = await _context.Sections
                .Where(s => s.InstructorId == instructor.Id)
                .Select(s => s.CourseId)
                .Distinct()
                .ToListAsync();

            var invalidCourseIds = request.CourseIds.Except(instructorCourseIds).ToList();
            if (invalidCourseIds.Any())
            {
                return BadRequest(ApiResponse<object>.CreateError("INVALID_COURSE_ID", "One or more course IDs do not belong to this instructor."));
            }

            var announcement = new Announcement
            {
                Title = request.Title,
                Body = request.Body,
                CreatedById = userId,
                InstructorId = instructor.Id,
                Status = request.Status,
                ScheduledAt = request.ScheduledAt,
                SendPush = request.SendPush,
                AttachmentUrl = request.AttachmentUrl,
                Audience = request.Audience
            };

            foreach (var courseId in request.CourseIds)
            {
                announcement.AnnouncementCourses.Add(new AnnouncementCourse { CourseId = courseId });
            }

            _context.Announcements.Add(announcement);
            await _context.SaveChangesAsync();

            var responseObj = new
            {
                announcement = new
                {
                    id = announcement.Id,
                    title = announcement.Title,
                    body = announcement.Body,
                    course_ids = announcement.AnnouncementCourses.Select(ac => ac.CourseId).ToList(),
                    audience = announcement.Audience,
                    send_push = announcement.SendPush,
                    has_attachment = !string.IsNullOrEmpty(announcement.AttachmentUrl),
                    status = announcement.Status,
                    scheduled_at = announcement.ScheduledAt,
                    published_at = announcement.Status == "published" ? announcement.CreatedAt : (DateTimeOffset?)null,
                    notified_students_count = 0
                }
            };

            return Created("", responseObj);
        }


    }
}
