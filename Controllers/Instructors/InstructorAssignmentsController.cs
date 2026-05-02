using IbnElgm3a.Models;
using IbnElgm3a.Models.Data;
using IbnElgm3a.Services;
using IbnElgm3a.Services.Localization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using IbnElgm3a.DTOs.Academics;

namespace IbnElgm3a.Controllers.Instructors
{
    [ApiController]
    [Route("instructor/assignments")]
    [Authorize(Roles = "instructor")]
    public class InstructorAssignmentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILocalizationService _localizer;
        private readonly INotificationService _notificationService;

        public InstructorAssignmentsController(AppDbContext context, ILocalizationService localizer, INotificationService notificationService)
        {
            _context = context;
            _localizer = localizer;
            _notificationService = notificationService;
        }

        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        [HttpGet("courses/{course_id}/assignments")]
        public async Task<IActionResult> GetAssignments(string course_id)
        {
            var userId = GetUserId();
            var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.UserId == userId);
            if (instructor == null) return Unauthorized();

            var isTeaching = await _context.Sections.AnyAsync(s => s.CourseId == course_id && s.InstructorId == instructor.Id);
            if (!isTeaching) return Forbid();

            var assignments = await _context.Assignments
                .Where(a => a.CourseId == course_id)
                .OrderByDescending(a => a.DueDate)
                .Select(a => new
                {
                    id = a.Id,
                    title = a.Title,
                    due_date = a.DueDate,
                    max_points = a.MaxPoints,
                    status = a.Status,
                    submission_count = _context.AssignmentSubmissions.Count(s => s.AssignmentId == a.Id),
                    graded_count = _context.AssignmentSubmissions.Count(s => s.AssignmentId == a.Id && s.Status == "graded")
                })
                .ToListAsync();

            return Ok(new { data = assignments });
        }

        [HttpPost("courses/{course_id}/assignments")]
        public async Task<IActionResult> CreateAssignment(string course_id, [FromBody] AssignmentRequest request)
        {
            var userId = GetUserId();
            var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.UserId == userId);
            if (instructor == null) return Unauthorized();

            var isTeaching = await _context.Sections.AnyAsync(s => s.CourseId == course_id && s.InstructorId == instructor.Id);
            if (!isTeaching) return Forbid();

            var assignment = new Assignment
            {
                CourseId = course_id,
                Title = request.Title,
                Description = request.Description,
                DueDate = request.DueDate,
                MaxPoints = request.MaxPoints,
                Status = "published",
                AllowLateSubmissions = request.AllowLateSubmissions,
                AttachmentUrl = request.AttachmentUrl
            };

            _context.Assignments.Add(assignment);
            await _context.SaveChangesAsync();

            return Created("", new { data = assignment });
        }

        [HttpPatch("assignments/{assignment_id}")]
        public async Task<IActionResult> UpdateAssignment(string assignment_id, [FromBody] AssignmentRequest request)
        {
            var userId = GetUserId();
            var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.UserId == userId);
            if (instructor == null) return Unauthorized();

            var assignment = await _context.Assignments.Include(a => a.Course).FirstOrDefaultAsync(a => a.Id == assignment_id);
            if (assignment == null) return NotFound();

            var isTeaching = await _context.Sections.AnyAsync(s => s.CourseId == assignment.CourseId && s.InstructorId == instructor.Id);
            if (!isTeaching) return Forbid();

            if (!string.IsNullOrEmpty(request.Title)) assignment.Title = request.Title;
            if (!string.IsNullOrEmpty(request.Description)) assignment.Description = request.Description;
            if (request.DueDate != default) assignment.DueDate = request.DueDate;
            if (request.MaxPoints > 0) assignment.MaxPoints = request.MaxPoints;
            assignment.AllowLateSubmissions = request.AllowLateSubmissions;
            if (!string.IsNullOrEmpty(request.AttachmentUrl)) assignment.AttachmentUrl = request.AttachmentUrl;

            await _context.SaveChangesAsync();
            return Ok(new { data = assignment });
        }

        [HttpGet("assignments/{assignment_id}/submissions")]
        public async Task<IActionResult> GetSubmissions(string assignment_id)
        {
            var userId = GetUserId();
            var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.UserId == userId);
            if (instructor == null) return Unauthorized();

            var assignment = await _context.Assignments.FindAsync(assignment_id);
            if (assignment == null) return NotFound();

            var isTeaching = await _context.Sections.AnyAsync(s => s.CourseId == assignment.CourseId && s.InstructorId == instructor.Id);
            if (!isTeaching) return Forbid();

            var submissions = await _context.AssignmentSubmissions
                .Include(s => s.Student)
                    .ThenInclude(st => st!.User)
                .Where(s => s.AssignmentId == assignment_id)
                .Select(s => new
                {
                    id = s.Id,
                    student_id = s.StudentId,
                    student_name = s.Student!.User!.Name,
                    submitted_at = s.SubmittedAt,
                    status = s.Status,
                    score = s.Score
                })
                .ToListAsync();

            return Ok(new { data = submissions });
        }

        [HttpGet("submissions/{submission_id}")]
        public async Task<IActionResult> GetSubmissionDetail(string submission_id)
        {
            var userId = GetUserId();
            var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.UserId == userId);
            if (instructor == null) return Unauthorized();

            var submission = await _context.AssignmentSubmissions
                .Include(s => s.Student)
                    .ThenInclude(st => st!.User)
                .Include(s => s.Assignment)
                .FirstOrDefaultAsync(s => s.Id == submission_id);

            if (submission == null) return NotFound();

            var isTeaching = await _context.Sections.AnyAsync(s => s.CourseId == submission.Assignment!.CourseId && s.InstructorId == instructor.Id);
            if (!isTeaching) return Forbid();

            return Ok(new { data = submission });
        }

        [HttpPut("submissions/{submission_id}/grade")]
        public async Task<IActionResult> GradeSubmission(string submission_id, [FromBody] GradeRequest request)
        {
            var userId = GetUserId();
            var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.UserId == userId);
            if (instructor == null) return Unauthorized();

            var submission = await _context.AssignmentSubmissions.Include(s => s.Assignment).FirstOrDefaultAsync(s => s.Id == submission_id);
            if (submission == null) return NotFound();

            var isTeaching = await _context.Sections.AnyAsync(s => s.CourseId == submission.Assignment!.CourseId && s.InstructorId == instructor.Id);
            if (!isTeaching) return Forbid();

            submission.Score = request.Score;
            submission.Feedback = request.Feedback;
            submission.Status = "graded";

            await _context.SaveChangesAsync();
            return Ok(new { data = submission });
        }

        [HttpPost("assignments/{assignment_id}/grades/publish")]
        public async Task<IActionResult> PublishGrades(string assignment_id)
        {
            var userId = GetUserId();
            var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.UserId == userId);
            if (instructor == null) return Unauthorized();

            var assignment = await _context.Assignments.FindAsync(assignment_id);
            if (assignment == null) return NotFound();

            var isTeaching = await _context.Sections.AnyAsync(s => s.CourseId == assignment.CourseId && s.InstructorId == instructor.Id);
            if (!isTeaching) return Forbid();

            assignment.GradesPublished = true;

            var submissions = await _context.AssignmentSubmissions
                .Include(s => s.Student)
                .Where(s => s.AssignmentId == assignment_id && s.Status == "graded")
                .ToListAsync();

            foreach (var sub in submissions)
            {
                await _notificationService.CreateNotificationAsync(
                    sub.StudentId,
                    "assignment_graded",
                    _localizer.GetMessage("ASSIGNMENT_GRADED_TITLE"),
                    string.Format(_localizer.GetMessage("ASSIGNMENT_GRADED_BODY"), assignment.Title, sub.Score, assignment.MaxPoints),
                    $"/student/assignments/{assignment.Id}"
                );
            }

            await _context.SaveChangesAsync();
            return Ok(new { data = new { message = _localizer.GetMessage("GRADES_PUBLISHED") } });
        }


    }
}
