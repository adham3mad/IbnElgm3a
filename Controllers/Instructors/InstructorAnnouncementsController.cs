using IbnElgm3a.Models;
using IbnElgm3a.Models.Data;
using IbnElgm3a.Services;
using IbnElgm3a.Services.Localization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using IbnElgm3a.DTOs.Announcements;
using IbnElgm3a.Enums;

namespace IbnElgm3a.Controllers.Instructors
{
    [ApiController]
    [Route("instructor/announcements")]
    [Authorize(Roles = "instructor")]
    public class InstructorAnnouncementsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILocalizationService _localizer;
        private readonly IFileStorageService _fileStorage;
        private readonly INotificationService _notificationService;

        public InstructorAnnouncementsController(
            AppDbContext context, 
            ILocalizationService localizer,
            IFileStorageService fileStorage,
            INotificationService notificationService)
        {
            _context = context;
            _localizer = localizer;
            _fileStorage = fileStorage;
            _notificationService = notificationService;
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
                    attachment_url = a.AttachmentUrl,
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
                    a.attachment_url,
                    a.status,
                    a.scheduled_at,
                    a.published_at,
                    a.notified_students_count
                })
                .ToList();

            return Ok(new { announcements = announcements });
        }

        [HttpPost]
        public async Task<IActionResult> CreateAnnouncement([FromForm] AnnouncementRequest request)
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

            string? attachmentUrl = null;
            if (request.File != null)
            {
                attachmentUrl = await _fileStorage.SaveFileAsync(request.File, "uploads/announcements");
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
                AttachmentUrl = attachmentUrl,
                Audience = request.Audience
            };

            foreach (var courseId in request.CourseIds)
            {
                announcement.AnnouncementCourses.Add(new AnnouncementCourse { CourseId = courseId });
            }

            _context.Announcements.Add(announcement);
            await _context.SaveChangesAsync();

            if (announcement.Status == "published")
            {
                await NotifyStudentsAsync(announcement, request.CourseIds);
            }

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
                    attachment_url = announcement.AttachmentUrl,
                    status = announcement.Status,
                    scheduled_at = announcement.ScheduledAt,
                    published_at = announcement.Status == "published" ? announcement.CreatedAt : (DateTimeOffset?)null,
                    notified_students_count = announcement.SentCount
                }
            };

            return Created("", responseObj);
        }

        [HttpPatch("{announcement_id}")]
        public async Task<IActionResult> UpdateAnnouncement(string announcement_id, [FromForm] UpdateAnnouncementRequest request)
        {
            var userId = GetUserId();
            var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.UserId == userId);
            if (instructor == null) return Unauthorized();

            var announcement = await _context.Announcements
                .Include(a => a.AnnouncementCourses)
                .FirstOrDefaultAsync(a => a.Id == announcement_id && a.CreatedById == userId);

            if (announcement == null) return NotFound();

            if (request.CourseIds != null && request.CourseIds.Any())
            {
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

                // Update courses
                _context.RemoveRange(announcement.AnnouncementCourses);
                foreach (var courseId in request.CourseIds)
                {
                    announcement.AnnouncementCourses.Add(new AnnouncementCourse { CourseId = courseId });
                }
            }

            if (request.Title != null) announcement.Title = request.Title;
            if (request.Body != null) announcement.Body = request.Body;
            if (request.Audience != null) announcement.Audience = request.Audience;
            if (request.SendPush.HasValue) announcement.SendPush = request.SendPush.Value;
            if (request.ScheduledAt.HasValue) announcement.ScheduledAt = request.ScheduledAt.Value;

            bool statusChangedToPublished = false;
            if (request.Status != null)
            {
                if (request.Status == "published" && announcement.Status != "published")
                {
                    statusChangedToPublished = true;
                }
                announcement.Status = request.Status;
            }

            if (request.File != null)
            {
                // Delete old file if exists
                if (!string.IsNullOrEmpty(announcement.AttachmentUrl))
                {
                    try
                    {
                        await _fileStorage.DeleteFileAsync(announcement.AttachmentUrl, "uploads/announcements");
                    }
                    catch
                    {
                        // Ignore deletion errors for robust execution
                    }
                }

                var fileUrl = await _fileStorage.SaveFileAsync(request.File, "uploads/announcements");
                announcement.AttachmentUrl = fileUrl;
            }

            await _context.SaveChangesAsync();

            // Notify students if newly published
            if (statusChangedToPublished || (announcement.Status == "published" && request.CourseIds != null))
            {
                var courseIdsToNotify = announcement.AnnouncementCourses.Select(ac => ac.CourseId).ToList();
                await NotifyStudentsAsync(announcement, courseIdsToNotify);
            }

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
                    attachment_url = announcement.AttachmentUrl,
                    status = announcement.Status,
                    scheduled_at = announcement.ScheduledAt,
                    published_at = announcement.Status == "published" ? announcement.CreatedAt : (DateTimeOffset?)null,
                    notified_students_count = announcement.SentCount
                }
            };

            return Ok(responseObj);
        }

        private async Task NotifyStudentsAsync(Announcement announcement, List<string> courseIds)
        {
            var studentIds = await _context.Enrollments
                .AsNoTracking()
                .Where(e => courseIds.Contains(e.Section!.CourseId) && e.Status == EnrollmentStatus.Enrolled)
                .Select(e => e.StudentId)
                .Distinct()
                .ToListAsync();

            if (studentIds.Any())
            {
                foreach (var studentId in studentIds)
                {
                    var notification = new Notification
                    {
                        StudentId = studentId,
                        Type = "announcement",
                        Title = announcement.Title,
                        Body = announcement.Body,
                        ActionUrl = $"/student/announcements",
                        IsRead = false
                    };
                    _context.Notifications.Add(notification);
                }

                announcement.SentCount = studentIds.Count;
                await _context.SaveChangesAsync();

                foreach (var studentId in studentIds)
                {
                    try
                    {
                        await _notificationService.InvalidateCacheAsync(studentId, isStudent: true);
                    }
                    catch
                    {
                        // Ignore cache invalidation failures during E2E/seeding
                    }
                }
            }
        }


    }
}
