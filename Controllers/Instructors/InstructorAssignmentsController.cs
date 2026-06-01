using IbnElgm3a.Models;
using IbnElgm3a.Models.Data;
using IbnElgm3a.Enums;
using IbnElgm3a.Services;
using IbnElgm3a.Services.Localization;
using IbnElgm3a.Attributes;
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
        public async Task<IActionResult> GetAssignments(string course_id, [FromQuery] AssignmentStatus? status)
        {
            var userId = GetUserId();
            var instructor = await _context.Instructors.AsNoTracking().FirstOrDefaultAsync(i => i.UserId == userId);
            if (instructor == null) return Unauthorized();

            var isTeaching = await _context.Sections.AsNoTracking().AnyAsync(s => s.CourseId == course_id && s.InstructorId == instructor.Id);
            if (!isTeaching) return Forbid();

            var enrolledCount = await _context.Enrollments
                .AsNoTracking()
                .CountAsync(e => e.Section!.CourseId == course_id && e.Status == Enums.EnrollmentStatus.Enrolled);

            var query = _context.Assignments.AsNoTracking().Where(a => a.CourseId == course_id);

            if (status.HasValue)
            {
                // Fix: pre-compute the string constant so PostgreSQL can translate the comparison
                var statusStr = status.Value.ToString().ToLower();
                query = query.Where(a => a.Status == statusStr);
            }

            var rawAssignments = await query
                .Select(a => new
                {
                    id = a.Id,
                    title = a.Title,
                    instructions = a.Description,
                    due_date = a.DueDate,
                    max_points = a.MaxPoints,
                    submission_type = "file",
                    allow_late_submissions = a.AllowLateSubmissions,
                    late_penalty_enabled = false,
                    status = a.Status,
                    submissions_count = _context.AssignmentSubmissions.Count(s => s.AssignmentId == a.Id),
                    total_enrolled = enrolledCount,
                    to_grade_count = _context.AssignmentSubmissions.Count(s => s.AssignmentId == a.Id && s.Status == "submitted"),
                    average_score = _context.AssignmentSubmissions
                        .Where(s => s.AssignmentId == a.Id && s.Status == "graded")
                        .Select(s => (double?)s.Score)
                        .Average(),
                    created_at = a.CreatedAt
                })
                .ToListAsync();

            // Order: published first (by due_date ASC), then closed (by due_date DESC), then draft (by created_at DESC)
            var orderedAssignments = rawAssignments
                .OrderBy(a => a.status == "published" ? 0 : (a.status == "closed" ? 1 : 2))
                .ThenBy(a => a.status == "published" ? a.due_date : DateTime.MaxValue)
                .ThenByDescending(a => a.status == "closed" ? a.due_date : DateTime.MinValue)
                .ThenByDescending(a => a.status == "draft" ? a.created_at : DateTimeOffset.MinValue)
                .Select(a => new
                {
                    a.id,
                    a.title,
                    a.instructions,
                    due_date = a.due_date.ToString("yyyy-MM-dd"),
                    a.max_points,
                    a.submission_type,
                    a.allow_late_submissions,
                    a.late_penalty_enabled,
                    a.status,
                    a.submissions_count,
                    a.total_enrolled,
                    a.to_grade_count,
                    a.average_score,
                    a.created_at
                })
                .ToList();

            return Ok(new { assignments = orderedAssignments });
        }

        [HttpPost("courses/{course_id}/assignments")]
        public async Task<IActionResult> CreateAssignment(string course_id, [FromBody] AssignmentRequest request)
        {
            var userId = GetUserId();
            var instructor = await _context.Instructors.AsNoTracking().FirstOrDefaultAsync(i => i.UserId == userId);
            if (instructor == null) return Unauthorized();

            var isTeaching = await _context.Sections.AsNoTracking().AnyAsync(s => s.CourseId == course_id && s.InstructorId == instructor.Id);
            if (!isTeaching) return Forbid();

            var assignment = new Assignment
            {
                CourseId = course_id,
                Title = request.Title,
                Description = request.Description ?? "",
                DueDate = DateTime.SpecifyKind(request.DueDate, DateTimeKind.Utc),
                MaxPoints = request.MaxPoints,
                Status = "published",
                AllowLateSubmissions = request.AllowLateSubmissions,
                AttachmentUrl = request.AttachmentUrl
            };

            _context.Assignments.Add(assignment);
            await _context.SaveChangesAsync();

            var responseObj = new
            {
                assignment = new
                {
                    id = assignment.Id,
                    title = assignment.Title,
                    instructions = assignment.Description,
                    due_date = assignment.DueDate.ToString("yyyy-MM-dd"),
                    max_points = assignment.MaxPoints,
                    submission_type = "file",
                    allow_late_submissions = assignment.AllowLateSubmissions,
                    late_penalty_enabled = false,
                    status = assignment.Status,
                    submissions_count = 0,
                    total_enrolled = await _context.Enrollments.CountAsync(e => e.Section!.CourseId == course_id && e.Status == Enums.EnrollmentStatus.Enrolled),
                    to_grade_count = 0,
                    average_score = (double?)null,
                    created_at = assignment.CreatedAt
                }
            };

            return Created("", responseObj);
        }

        [HttpPatch("assignments/{assignment_id}")]
        public async Task<IActionResult> UpdateAssignment(string assignment_id, [FromBody] AssignmentRequest request)
        {
            var userId = GetUserId();
            var instructor = await _context.Instructors.AsNoTracking().FirstOrDefaultAsync(i => i.UserId == userId);
            if (instructor == null) return Unauthorized();

            var assignment = await _context.Assignments.Include(a => a.Course).FirstOrDefaultAsync(a => a.Id == assignment_id);
            if (assignment == null) return NotFound();

            var isTeaching = await _context.Sections.AsNoTracking().AnyAsync(s => s.CourseId == assignment.CourseId && s.InstructorId == instructor.Id);
            if (!isTeaching) return Forbid();

            if (!string.IsNullOrEmpty(request.Title)) assignment.Title = request.Title;
            if (request.Description != null) assignment.Description = request.Description;
            if (request.DueDate != default) assignment.DueDate = DateTime.SpecifyKind(request.DueDate, DateTimeKind.Utc);
            if (request.MaxPoints > 0) assignment.MaxPoints = request.MaxPoints;
            assignment.AllowLateSubmissions = request.AllowLateSubmissions;
            if (request.AttachmentUrl != null) assignment.AttachmentUrl = request.AttachmentUrl;

            await _context.SaveChangesAsync();

            var enrolledCount = await _context.Enrollments.CountAsync(e => e.Section!.CourseId == assignment.CourseId && e.Status == Enums.EnrollmentStatus.Enrolled);
            var responseObj = new
            {
                assignment = new
                {
                    id = assignment.Id,
                    title = assignment.Title,
                    instructions = assignment.Description,
                    due_date = assignment.DueDate.ToString("yyyy-MM-dd"),
                    max_points = assignment.MaxPoints,
                    submission_type = "file",
                    allow_late_submissions = assignment.AllowLateSubmissions,
                    late_penalty_enabled = false,
                    status = assignment.Status,
                    submissions_count = await _context.AssignmentSubmissions.CountAsync(s => s.AssignmentId == assignment.Id),
                    total_enrolled = enrolledCount,
                    to_grade_count = await _context.AssignmentSubmissions.CountAsync(s => s.AssignmentId == assignment.Id && s.Status == "submitted"),
                    average_score = await _context.AssignmentSubmissions
                        .Where(s => s.AssignmentId == assignment.Id && s.Status == "graded")
                        .Select(s => (double?)s.Score)
                        .AverageAsync(),
                    created_at = assignment.CreatedAt
                }
            };

            return Ok(responseObj);
        }

        [HttpGet("assignments/{assignment_id}/submissions")]
        [BypassResponseWrapper]
        public async Task<IActionResult> GetSubmissions(string assignment_id, [FromQuery] SubmissionStatus? status, [FromQuery] int page = 1, [FromQuery] int limit = 30)
        {
            var userId = GetUserId();
            var instructor = await _context.Instructors.AsNoTracking().FirstOrDefaultAsync(i => i.UserId == userId);
            if (instructor == null) return Unauthorized();

            var assignment = await _context.Assignments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == assignment_id);
            if (assignment == null) return NotFound();

            var isTeaching = await _context.Sections.AsNoTracking().AnyAsync(s => s.CourseId == assignment.CourseId && s.InstructorId == instructor.Id);
            if (!isTeaching) return Forbid();

            var enrollments = await _context.Enrollments
                .AsNoTracking()
                .Where(e => e.Section!.CourseId == assignment.CourseId && e.Status == Enums.EnrollmentStatus.Enrolled)
                .Select(e => new
                {
                    StudentId = e.Student!.Id,
                    UserName = e.Student.User != null ? e.Student.User.Name : ""
                })
                .ToListAsync();

            var dbSubmissions = await _context.AssignmentSubmissions
                .AsNoTracking()
                .Where(s => s.AssignmentId == assignment_id)
                .ToListAsync();

            var submissionsList = enrollments.Select(e =>
            {
                var sub = dbSubmissions.FirstOrDefault(s => s.StudentId == e.StudentId);
                
                string subStatus = "missing";
                bool isLate = false;
                string? fileName = null;
                int? score = null;
                DateTimeOffset? submittedAt = null;

                if (sub != null)
                {
                    subStatus = sub.Status; // "submitted" or "graded"
                    score = sub.Score;
                    submittedAt = sub.SubmittedAt;
                    isLate = sub.SubmittedAt > assignment.DueDate;
                    if (isLate && subStatus == "submitted")
                    {
                        subStatus = "late";
                    }
                    fileName = !string.IsNullOrEmpty(sub.FileUrl) ? Path.GetFileName(sub.FileUrl) : null;
                }

                return new
                {
                    id = sub?.Id,
                    student = new
                    {
                        id = e.StudentId,
                        full_name = e.UserName,
                        initials = e.UserName.Length > 0
                            ? e.UserName.Substring(0, 1) + (e.UserName.Contains(' ') ? e.UserName.Split(' ')[1].Substring(0, 1) : "")
                            : ""
                    },
                    submitted_at = submittedAt,
                    is_late = isLate,
                    file_name = fileName,
                    score = score,
                    status = subStatus
                };
            }).ToList();

            if (status.HasValue)
            {
                var statusStr = status.Value.ToString().ToLower();
                submissionsList = submissionsList.Where(s => s.status.ToLower() == statusStr.ToLower()).ToList();
            }

            // Order: ungraded first (submitted, late), then graded, then missing. Within each group, ordered by submitted_at ASC.
            var orderedSubmissions = submissionsList
                .OrderBy(s => s.status == "submitted" || s.status == "late" ? 0 : (s.status == "graded" ? 1 : 2))
                .ThenBy(s => s.submitted_at ?? DateTimeOffset.MaxValue)
                .ToList();

            var totalItems = orderedSubmissions.Count;
            var pagedSubmissions = orderedSubmissions.Skip((page - 1) * limit).Take(limit).ToList();

            var totalEnrolled = enrollments.Count;
            var submissionsCount = dbSubmissions.Count;
            var toGradeCount = dbSubmissions.Count(s => s.Status == "submitted");
            var missingCount = totalEnrolled - submissionsCount;

            var assignmentHeader = new
            {
                id = assignment.Id,
                title = assignment.Title,
                due_date = assignment.DueDate.ToString("yyyy-MM-dd"),
                max_points = assignment.MaxPoints,
                submissions_count = submissionsCount,
                missing_count = missingCount,
                to_grade_count = toGradeCount,
                total_enrolled = totalEnrolled
            };

            return Ok(new
            {
                data = new
                {
                    assignment = assignmentHeader,
                    submissions = pagedSubmissions
                },
                meta = new
                {
                    page = page,
                    limit = limit,
                    total_items = totalItems,
                    total_pages = (int)Math.Ceiling((double)totalItems / limit)
                }
            });
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

            var isLate = submission.SubmittedAt > submission.Assignment!.DueDate;
            var responseObj = new
            {
                submission = new
                {
                    id = submission.Id,
                    assignment = new
                    {
                        id = submission.Assignment.Id,
                        title = submission.Assignment.Title,
                        max_points = submission.Assignment.MaxPoints,
                        rubric_criteria = new List<object>()
                    },
                    student = new
                    {
                        id = submission.Student!.Id,
                        full_name = submission.Student.User!.Name,
                        initials = submission.Student.User.Name.Substring(0, 1) + (submission.Student.User.Name.Contains(' ') ? submission.Student.User.Name.Split(' ')[1].Substring(0, 1) : ""),
                        student_number = submission.Student.AcademicNumber
                    },
                    submitted_at = submission.SubmittedAt,
                    is_late = isLate,
                    file_name = !string.IsNullOrEmpty(submission.FileUrl) ? Path.GetFileName(submission.FileUrl) : null,
                    file_url = submission.FileUrl,
                    file_size_bytes = (int?)null,
                    score = submission.Score,
                    feedback = submission.Feedback,
                    rubric_scores = new List<object>(),
                    status = isLate && submission.Status == "submitted" ? "late" : submission.Status
                }
            };

            return Ok(responseObj);
        }

        [HttpPatch("submissions/{submission_id}/grade")]
        public async Task<IActionResult> GradeSubmission(string submission_id, [FromBody] GradeRequest request)
        {
            var userId = GetUserId();
            var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.UserId == userId);
            if (instructor == null) return Unauthorized();

            var submission = await _context.AssignmentSubmissions
                .Include(s => s.Assignment)
                .Include(s => s.Student)
                    .ThenInclude(st => st!.User)
                .FirstOrDefaultAsync(s => s.Id == submission_id);

            if (submission == null) return NotFound();

            var isTeaching = await _context.Sections.AnyAsync(s => s.CourseId == submission.Assignment!.CourseId && s.InstructorId == instructor.Id);
            if (!isTeaching) return Forbid();

            if (request.Score < 0 || request.Score > submission.Assignment!.MaxPoints)
            {
                return BadRequest(ApiResponse<object>.CreateError("VALIDATION_ERROR", "Score is out of range."));
            }

            submission.Score = request.Score;
            submission.Feedback = request.Feedback;
            submission.Status = "graded";

            await _context.SaveChangesAsync();

            var isLate = submission.SubmittedAt > submission.Assignment!.DueDate;
            var responseObj = new
            {
                submission = new
                {
                    id = submission.Id,
                    assignment = new
                    {
                        id = submission.Assignment.Id,
                        title = submission.Assignment.Title,
                        max_points = submission.Assignment.MaxPoints,
                        rubric_criteria = new List<object>()
                    },
                    student = new
                    {
                        id = submission.Student!.Id,
                        full_name = submission.Student.User!.Name,
                        initials = submission.Student.User.Name.Substring(0, 1) + (submission.Student.User.Name.Contains(' ') ? submission.Student.User.Name.Split(' ')[1].Substring(0, 1) : ""),
                        student_number = submission.Student.AcademicNumber
                    },
                    submitted_at = submission.SubmittedAt,
                    is_late = isLate,
                    file_name = !string.IsNullOrEmpty(submission.FileUrl) ? Path.GetFileName(submission.FileUrl) : null,
                    file_url = submission.FileUrl,
                    file_size_bytes = (int?)null,
                    score = submission.Score,
                    feedback = submission.Feedback,
                    rubric_scores = new List<object>(),
                    status = isLate && submission.Status == "submitted" ? "late" : submission.Status
                }
            };

            return Ok(responseObj);
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

            var submissions = await _context.AssignmentSubmissions
                .Include(s => s.Student)
                .Where(s => s.AssignmentId == assignment_id && s.Status == "graded")
                .ToListAsync();

            if (!submissions.Any())
            {
                return StatusCode(422, ApiResponse<object>.CreateError("NO_GRADES_TO_PUBLISH", "No finalized grades exist for this assignment."));
            }

            int publishedCount = 0;
            int notifiedCount = 0;

            foreach (var sub in submissions)
            {
                await _notificationService.CreateNotificationAsync(
                    sub.StudentId,
                    "assignment_graded",
                    _localizer.GetMessage("ASSIGNMENT_GRADED_TITLE"),
                    string.Format(_localizer.GetMessage("ASSIGNMENT_GRADED_BODY"), assignment.Title, sub.Score, assignment.MaxPoints),
                    $"/student/assignments/{assignment.Id}"
                );
                publishedCount++;
                notifiedCount++;
            }

            assignment.GradesPublished = true;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                published_count = publishedCount,
                notified_students_count = notifiedCount
            });
        }
    }
}
