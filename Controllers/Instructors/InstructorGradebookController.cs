using IbnElgm3a.Models;
using IbnElgm3a.Models.Data;
using IbnElgm3a.Services;
using IbnElgm3a.Services.Localization;
using IbnElgm3a.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IbnElgm3a.Controllers.Instructors
{
    [ApiController]
    [Route("instructor/gradebook")]
    [Authorize(Roles = "instructor")]
    public class InstructorGradebookController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILocalizationService _localizer;

        public InstructorGradebookController(AppDbContext context, ILocalizationService localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        private string MapToLetterGrade(double? avg)
        {
            if (avg == null) return null!;
            var val = avg.Value;
            if (val >= 0.90) return "A";
            if (val >= 0.85) return "A-";
            if (val >= 0.80) return "B+";
            if (val >= 0.75) return "B";
            if (val >= 0.70) return "B-";
            if (val >= 0.65) return "C+";
            if (val >= 0.60) return "C";
            if (val >= 0.55) return "C-";
            if (val >= 0.50) return "D";
            return "F";
        }

        [HttpGet("courses/{course_id}/gradebook")]
        public async Task<IActionResult> GetGradebook(string course_id, [FromQuery] int page = 1, [FromQuery] int limit = 30)
        {
            var userId = GetUserId();
            var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.UserId == userId);
            if (instructor == null) return Unauthorized();

            var isTeaching = await _context.Sections.AnyAsync(s => s.CourseId == course_id && s.InstructorId == instructor.Id);
            if (!isTeaching) return Forbid();

            var enrollments = await _context.Enrollments
                .Include(e => e.Student)
                    .ThenInclude(s => s!.User)
                .Where(e => e.Section!.CourseId == course_id && e.Status == Enums.EnrollmentStatus.Enrolled)
                .ToListAsync();

            var assignments = await _context.Assignments
                .Where(a => a.CourseId == course_id)
                .ToListAsync();

            var assignmentIds = assignments.Select(a => a.Id).ToList();
            var assignmentSubmissions = await _context.AssignmentSubmissions
                .Where(s => assignmentIds.Contains(s.AssignmentId))
                .ToListAsync();

            var quizzes = await _context.Quizzes
                .Include(q => q.Questions)
                .Where(q => q.CourseId == course_id)
                .ToListAsync();

            var quizIds = quizzes.Select(q => q.Id).ToList();
            var quizSubmissions = await _context.QuizSubmissions
                .Where(s => quizIds.Contains(s.QuizId))
                .ToListAsync();

            var studentRows = enrollments.Select(e =>
            {
                var student = e.Student!;
                var initials = student.User!.Name.Substring(0, 1) + (student.User.Name.Contains(' ') ? student.User.Name.Split(' ')[1].Substring(0, 1) : "");

                var studentAssignmentSubs = assignmentSubmissions.Where(s => s.StudentId == student.Id && s.Status == "graded").ToList();
                var studentQuizSubs = quizSubmissions.Where(s => s.StudentId == student.Id && s.Status == "completed").ToList();

                int earnedScore = 0;
                int maxPossiblePoints = 0;

                foreach (var sub in studentAssignmentSubs)
                {
                    var ass = assignments.FirstOrDefault(a => a.Id == sub.AssignmentId);
                    if (ass != null)
                    {
                        earnedScore += sub.Score ?? 0;
                        maxPossiblePoints += ass.MaxPoints;
                    }
                }

                foreach (var sub in studentQuizSubs)
                {
                    var qz = quizzes.FirstOrDefault(q => q.Id == sub.QuizId);
                    if (qz != null)
                    {
                        earnedScore += sub.Score;
                        maxPossiblePoints += qz.Questions.Sum(q => q.Points);
                    }
                }

                double? overallAverage = null;
                if (maxPossiblePoints > 0)
                {
                    overallAverage = (double)earnedScore / maxPossiblePoints;
                }

                return new
                {
                    student = new
                    {
                        id = student.Id,
                        full_name = student.User.Name,
                        initials = initials,
                        student_number = student.AcademicNumber
                    },
                    overall_average = overallAverage,
                    letter_grade = MapToLetterGrade(overallAverage),
                    graded_assignments_count = studentAssignmentSubs.Count,
                    graded_quizzes_count = studentQuizSubs.Count,
                    last_name = student.User.Name.Contains(' ') ? student.User.Name.Split(' ')[1] : student.User.Name
                };
            }).ToList();

            // Order by overall_average DESC (nulls last), then last_name ASC
            var sortedStudents = studentRows
                .OrderByDescending(s => s.overall_average.HasValue)
                .ThenByDescending(s => s.overall_average ?? -1.0)
                .ThenBy(s => s.last_name)
                .Select(s => new
                {
                    s.student,
                    s.overall_average,
                    s.letter_grade,
                    s.graded_assignments_count,
                    s.graded_quizzes_count
                })
                .ToList();

            var totalItems = sortedStudents.Count;
            var pagedStudents = sortedStudents.Skip((page - 1) * limit).Take(limit).ToList();

            double? classAverage = null;
            var studentsWithAverages = sortedStudents.Where(s => s.overall_average.HasValue).ToList();
            if (studentsWithAverages.Any())
            {
                classAverage = studentsWithAverages.Average(s => s.overall_average.Value);
            }

            var awaitingGradeCount = await _context.AssignmentSubmissions
                .CountAsync(s => assignmentIds.Contains(s.AssignmentId) && s.Status == "submitted");

            return Ok(new
            {
                data = new
                {
                    course_id = course_id,
                    class_average = classAverage,
                    awaiting_grade_count = awaitingGradeCount,
                    students = pagedStudents
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

        [HttpGet("courses/{course_id}/students/{student_id}/report")]
        public async Task<IActionResult> GetStudentReport(string course_id, string student_id)
        {
            var userId = GetUserId();
            var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.UserId == userId);
            if (instructor == null) return Unauthorized();

            var isTeaching = await _context.Sections.AnyAsync(s => s.CourseId == course_id && s.InstructorId == instructor.Id);
            if (!isTeaching) return Forbid();

            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == student_id);

            if (student == null) return NotFound();

            var course = await _context.Courses.FindAsync(course_id);
            if (course == null) return NotFound();

            var assignments = await _context.Assignments
                .Where(a => a.CourseId == course_id)
                .OrderByDescending(a => a.DueDate)
                .ToListAsync();

            var assignmentIds = assignments.Select(a => a.Id).ToList();
            var submissions = await _context.AssignmentSubmissions
                .Where(s => s.StudentId == student_id && assignmentIds.Contains(s.AssignmentId))
                .ToListAsync();

            var quizzes = await _context.Quizzes
                .Include(q => q.Questions)
                .Where(q => q.CourseId == course_id)
                .OrderByDescending(q => q.EndDate)
                .ToListAsync();

            var quizIds = quizzes.Select(q => q.Id).ToList();
            var quizSubmissions = await _context.QuizSubmissions
                .Where(s => s.StudentId == student_id && quizIds.Contains(s.QuizId))
                .ToListAsync();

            var sessions = await _context.Sessions
                .Where(s => s.Section!.CourseId == course_id && s.AttendanceStatus == "completed")
                .OrderByDescending(s => s.Date)
                .ToListAsync();

            var sessionIds = sessions.Select(s => s.Id).ToList();
            var attendance = await _context.AttendanceRecords
                .Where(a => a.StudentId == student_id && sessionIds.Contains(a.SessionId))
                .ToListAsync();

            // Calculate student average
            int earnedScore = 0;
            int maxPossiblePoints = 0;

            var studentAssignmentSubs = submissions.Where(s => s.Status == "graded").ToList();
            foreach (var sub in studentAssignmentSubs)
            {
                var ass = assignments.FirstOrDefault(a => a.Id == sub.AssignmentId);
                if (ass != null)
                {
                    earnedScore += sub.Score ?? 0;
                    maxPossiblePoints += ass.MaxPoints;
                }
            }

            var studentQuizSubs = quizSubmissions.Where(s => s.Status == "completed").ToList();
            foreach (var sub in studentQuizSubs)
            {
                var qz = quizzes.FirstOrDefault(q => q.Id == sub.QuizId);
                if (qz != null)
                {
                    earnedScore += sub.Score;
                    maxPossiblePoints += qz.Questions.Sum(q => q.Points);
                }
            }

            double? overallAverage = null;
            if (maxPossiblePoints > 0)
            {
                overallAverage = (double)earnedScore / maxPossiblePoints;
            }

            var attendanceRate = sessions.Any() ? (double)attendance.Count(a => a.Status == "present" || a.Status == "late") / sessions.Count : 1.0;
            var riskStatus = attendanceRate >= 0.75 ? "good" : (attendanceRate >= 0.60 ? "watch" : "at_risk");

            var assignmentGrades = assignments.Select(a =>
            {
                var sub = submissions.FirstOrDefault(s => s.AssignmentId == a.Id);
                string status = "pending";
                int? score = null;
                DateTimeOffset? gradedAt = null;

                if (sub != null)
                {
                    status = sub.Status == "graded" ? "graded" : "pending";
                    score = sub.Score;
                    gradedAt = sub.Status == "graded" ? sub.UpdatedAt : (DateTimeOffset?)null;
                }
                else if (a.DueDate < DateTime.UtcNow)
                {
                    status = "missing";
                }

                return new
                {
                    assignment_id = a.Id,
                    title = a.Title,
                    max_points = a.MaxPoints,
                    score = score,
                    graded_at = gradedAt,
                    status = status
                };
            }).ToList();

            var quizResults = quizzes.Select(q =>
            {
                var sub = quizSubmissions.FirstOrDefault(s => s.QuizId == q.Id);
                string status = "not_attempted";
                int? score = null;
                int? timeTakenSeconds = null;
                DateTimeOffset? submittedAt = null;

                if (sub != null)
                {
                    status = "completed";
                    score = sub.Score;
                    timeTakenSeconds = sub.CompletedAt.HasValue ? (int)(sub.CompletedAt.Value - sub.StartedAt).TotalSeconds : (int?)null;
                    submittedAt = sub.CompletedAt;
                }

                return new
                {
                    quiz_id = q.Id,
                    title = q.Title,
                    max_points = q.Questions.Sum(qu => qu.Points),
                    score = score,
                    time_taken_seconds = timeTakenSeconds,
                    submitted_at = submittedAt,
                    status = status
                };
            }).ToList();

            var recentAttendance = sessions.Take(10).Select(s =>
            {
                var att = attendance.FirstOrDefault(a => a.SessionId == s.Id);
                return new
                {
                    session_id = s.Id,
                    session_number = s.SessionNumber,
                    date = s.Date.ToString("yyyy-MM-dd"),
                    status = att?.Status ?? "not_recorded"
                };
            }).ToList();

            var responseObj = new
            {
                student = new
                {
                    id = student.Id,
                    full_name = student.User!.Name,
                    initials = student.User.Name.Substring(0, 1) + (student.User.Name.Contains(' ') ? student.User.Name.Split(' ')[1].Substring(0, 1) : ""),
                    student_number = student.AcademicNumber
                },
                course = new
                {
                    id = course.Id,
                    code = course.CourseCode,
                    name = course.Title
                },
                overall_average = overallAverage,
                letter_grade = MapToLetterGrade(overallAverage),
                attendance_rate = attendanceRate,
                risk_status = riskStatus,
                assignment_grades = assignmentGrades,
                quiz_results = quizResults,
                recent_attendance = recentAttendance
            };

            return Ok(responseObj);
        }
    }
}
