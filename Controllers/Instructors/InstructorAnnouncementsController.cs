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

            var announcements = await query
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new
                {
                    id = a.Id,
                    title = a.Title,
                    body = a.Body,
                    created_at = a.CreatedAt,
                    status = a.Status,
                    course_ids = a.AnnouncementCourses.Select(ac => ac.CourseId).ToList(),
                    sent_count = a.SentCount,
                    read_count = a.ReadCount
                })
                .ToListAsync();

            return Ok(new { data = announcements });
        }

        [HttpPost]
        public async Task<IActionResult> CreateAnnouncement([FromBody] AnnouncementRequest request)
        {
            var userId = GetUserId();
            var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.UserId == userId);
            if (instructor == null) return Unauthorized();

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

            return Created("", new { data = announcement });
        }


    }
}
