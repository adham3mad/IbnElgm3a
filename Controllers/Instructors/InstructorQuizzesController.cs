using IbnElgm3a.Models;
using IbnElgm3a.Models.Data;
using IbnElgm3a.Services;
using IbnElgm3a.Services.Localization;
using IbnElgm3a.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using IbnElgm3a.DTOs.Academics;

namespace IbnElgm3a.Controllers.Instructors
{
    [ApiController]
    [Route("instructor/Quizzes")]
    [Authorize(Roles = "instructor")]
    public class InstructorQuizzesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILocalizationService _localizer;

        public InstructorQuizzesController(AppDbContext context, ILocalizationService localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        [HttpGet("courses/{course_id}/quizzes")]
        public async Task<IActionResult> GetQuizzes(string course_id, [FromQuery] string? status)
        {
            var userId = GetUserId();
            var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.UserId == userId);
            if (instructor == null) return Unauthorized();

            var isTeaching = await _context.Sections.AnyAsync(s => s.CourseId == course_id && s.InstructorId == instructor.Id);
            if (!isTeaching) return Forbid();

            var rawQuizzes = await _context.Quizzes
                .Include(q => q.Questions)
                .Where(q => q.CourseId == course_id)
                .ToListAsync();

            var mappedQuizzes = rawQuizzes.Select(q =>
            {
                var submissions = _context.QuizSubmissions.Where(s => s.QuizId == q.Id).ToList();
                var attemptsCount = submissions.Count;
                var completedSubmissions = submissions.Where(s => s.Status == "completed").ToList();
                double? avgScore = completedSubmissions.Any() ? completedSubmissions.Average(s => (double)s.Score) : (double?)null;

                return new
                {
                    id = q.Id,
                    title = q.Title,
                    time_limit_minutes = q.TimeLimitMinutes,
                    attempts_allowed = 1,
                    opens_at = q.StartDate,
                    closes_at = q.EndDate,
                    shuffle_questions = q.ShuffleQuestions,
                    status = q.Status,
                    question_count = q.Questions.Count,
                    total_points = q.Questions.Sum(qu => qu.Points),
                    attempts_count = attemptsCount,
                    average_score = avgScore,
                    created_at = q.CreatedAt
                };
            }).ToList();

            if (!string.IsNullOrEmpty(status) && status != "all")
            {
                mappedQuizzes = mappedQuizzes.Where(q => q.status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Order: published first (by closes_at ASC), then closed (by closes_at DESC), then draft (by created_at DESC)
            var orderedQuizzes = mappedQuizzes
                .OrderBy(q => q.status == "published" ? 0 : (q.status == "closed" ? 1 : 2))
                .ThenBy(q => q.status == "published" ? q.closes_at ?? DateTimeOffset.MaxValue : DateTimeOffset.MaxValue)
                .ThenByDescending(q => q.status == "closed" ? q.closes_at ?? DateTimeOffset.MinValue : DateTimeOffset.MinValue)
                .ThenByDescending(q => q.status == "draft" ? q.created_at : DateTimeOffset.MinValue)
                .Select(q => new
                {
                    q.id,
                    q.title,
                    q.time_limit_minutes,
                    q.attempts_allowed,
                    q.opens_at,
                    q.closes_at,
                    q.shuffle_questions,
                    q.status,
                    q.question_count,
                    q.total_points,
                    q.attempts_count,
                    q.average_score
                })
                .ToList();

            return Ok(new { quizzes = orderedQuizzes });
        }

        [HttpPost("courses/{course_id}/quizzes")]
        public async Task<IActionResult> CreateQuiz(string course_id, [FromBody] QuizRequest request)
        {
            var userId = GetUserId();
            var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.UserId == userId);
            if (instructor == null) return Unauthorized();

            var isTeaching = await _context.Sections.AnyAsync(s => s.CourseId == course_id && s.InstructorId == instructor.Id);
            if (!isTeaching) return Forbid();

            var quiz = new Quiz
            {
                CourseId = course_id,
                Title = request.Title,
                Description = request.Description ?? "",
                Status = request.Status,
                StartDate = request.OpensAt,
                EndDate = request.ClosesAt,
                TimeLimitMinutes = request.TimeLimitMinutes,
                ShuffleQuestions = request.ShuffleQuestions
            };

            _context.Quizzes.Add(quiz);
            await _context.SaveChangesAsync();

            if (request.Questions != null)
            {
                foreach (var q in request.Questions)
                {
                    var question = new QuizQuestion
                    {
                        QuizId = quiz.Id,
                        Type = q.Type,
                        Text = q.Text,
                        Points = q.Points,
                        OrderIndex = q.Order,
                        OptionsJson = q.Options != null ? JsonSerializer.Serialize(q.Options) : null,
                        CorrectOptionIndex = q.CorrectOptionIndex,
                        CorrectBoolean = q.CorrectBoolean
                    };
                    _context.QuizQuestions.Add(question);
                }
                await _context.SaveChangesAsync();
            }

            var responseObj = new
            {
                quiz = new
                {
                    id = quiz.Id,
                    title = quiz.Title,
                    description = quiz.Description,
                    time_limit_minutes = quiz.TimeLimitMinutes,
                    attempts_allowed = 1,
                    opens_at = quiz.StartDate,
                    closes_at = quiz.EndDate,
                    shuffle_questions = quiz.ShuffleQuestions,
                    status = quiz.Status,
                    questions = _context.QuizQuestions.Where(qu => qu.QuizId == quiz.Id).OrderBy(qu => qu.OrderIndex).Select(qu => new
                    {
                        id = qu.Id,
                        type = qu.Type,
                        text = qu.Text,
                        points = qu.Points,
                        order = qu.OrderIndex,
                        options = !string.IsNullOrEmpty(qu.OptionsJson) ? JsonSerializer.Deserialize<List<string>>(qu.OptionsJson, (JsonSerializerOptions)null!) : null,
                        correct_option_index = qu.CorrectOptionIndex,
                        correct_boolean = qu.CorrectBoolean
                    }).ToList()
                }
            };

            return Created("", responseObj);
        }

        [HttpPatch("quizzes/{quiz_id}")]
        public async Task<IActionResult> UpdateQuiz(string quiz_id, [FromBody] QuizRequest request)
        {
            var userId = GetUserId();
            var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.UserId == userId);
            if (instructor == null) return Unauthorized();

            var quiz = await _context.Quizzes.Include(q => q.Questions).FirstOrDefaultAsync(q => q.Id == quiz_id);
            if (quiz == null) return NotFound();

            var isTeaching = await _context.Sections.AnyAsync(s => s.CourseId == quiz.CourseId && s.InstructorId == instructor.Id);
            if (!isTeaching) return Forbid();

            var attemptsCount = await _context.QuizSubmissions.CountAsync(s => s.QuizId == quiz_id);

            if (quiz.Status == "published" && request.Status == "draft")
            {
                return StatusCode(422, ApiResponse<object>.CreateError("INVALID_STATUS_TRANSITION", "Attempted to revert quiz from published to draft."));
            }

            if (quiz.Status == "published" && attemptsCount > 0)
            {
                if (request.Questions != null && request.Questions.Any())
                {
                    return StatusCode(422, ApiResponse<object>.CreateError("QUIZ_HAS_ATTEMPTS", "Cannot modify questions on a quiz with submissions."));
                }
                if (!string.IsNullOrEmpty(request.Title)) quiz.Title = request.Title;
                if (request.ClosesAt != default) quiz.EndDate = request.ClosesAt;
                if (!string.IsNullOrEmpty(request.Status)) quiz.Status = request.Status;
            }
            else
            {
                if (!string.IsNullOrEmpty(request.Title)) quiz.Title = request.Title;
                if (request.Description != null) quiz.Description = request.Description;
                if (request.TimeLimitMinutes > 0) quiz.TimeLimitMinutes = request.TimeLimitMinutes;
                if (request.OpensAt != default) quiz.StartDate = request.OpensAt;
                if (request.ClosesAt != default) quiz.EndDate = request.ClosesAt;
                quiz.ShuffleQuestions = request.ShuffleQuestions;
                if (!string.IsNullOrEmpty(request.Status)) quiz.Status = request.Status;

                if (request.Questions != null)
                {
                    var oldQuestions = _context.QuizQuestions.Where(q => q.QuizId == quiz_id);
                    _context.QuizQuestions.RemoveRange(oldQuestions);

                    foreach (var q in request.Questions)
                    {
                        var question = new QuizQuestion
                        {
                            QuizId = quiz_id,
                            Type = q.Type,
                            Text = q.Text,
                            Points = q.Points,
                            OrderIndex = q.Order,
                            OptionsJson = q.Options != null ? JsonSerializer.Serialize(q.Options) : null,
                            CorrectOptionIndex = q.CorrectOptionIndex,
                            CorrectBoolean = q.CorrectBoolean
                        };
                        _context.QuizQuestions.Add(question);
                    }
                }
            }

            await _context.SaveChangesAsync();

            var responseObj = new
            {
                quiz = new
                {
                    id = quiz.Id,
                    title = quiz.Title,
                    description = quiz.Description,
                    time_limit_minutes = quiz.TimeLimitMinutes,
                    attempts_allowed = 1,
                    opens_at = quiz.StartDate,
                    closes_at = quiz.EndDate,
                    shuffle_questions = quiz.ShuffleQuestions,
                    status = quiz.Status,
                    questions = _context.QuizQuestions.Where(qu => qu.QuizId == quiz.Id).OrderBy(qu => qu.OrderIndex).Select(qu => new
                    {
                        id = qu.Id,
                        type = qu.Type,
                        text = qu.Text,
                        points = qu.Points,
                        order = qu.OrderIndex,
                        options = !string.IsNullOrEmpty(qu.OptionsJson) ? JsonSerializer.Deserialize<List<string>>(qu.OptionsJson, (JsonSerializerOptions)null!) : null,
                        correct_option_index = qu.CorrectOptionIndex,
                        correct_boolean = qu.CorrectBoolean
                    }).ToList()
                }
            };

            return Ok(responseObj);
        }

        [HttpGet("quizzes/{quiz_id}/results")]
        public async Task<IActionResult> GetQuizResults(string quiz_id)
        {
            var userId = GetUserId();
            var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.UserId == userId);
            if (instructor == null) return Unauthorized();

            var quiz = await _context.Quizzes.Include(q => q.Questions).FirstOrDefaultAsync(q => q.Id == quiz_id);
            if (quiz == null) return NotFound();

            var isTeaching = await _context.Sections.AnyAsync(s => s.CourseId == quiz.CourseId && s.InstructorId == instructor.Id);
            if (!isTeaching) return Forbid();

            int totalPoints = quiz.Questions.Sum(q => q.Points);
            var submissions = await _context.QuizSubmissions
                .Where(s => s.QuizId == quiz_id && s.Status == "completed")
                .ToListAsync();

            var studentResultsRaw = await _context.Enrollments
                .Include(e => e.Student)
                    .ThenInclude(s => s!.User)
                .Where(e => e.Section!.CourseId == quiz.CourseId && e.Status == Enums.EnrollmentStatus.Enrolled)
                .ToListAsync();

            var studentResults = studentResultsRaw.Select(e =>
            {
                var student = e.Student!;
                var initials = student.User!.Name.Substring(0, 1) + (student.User.Name.Contains(' ') ? student.User.Name.Split(' ')[1].Substring(0, 1) : "");
                var sub = submissions.FirstOrDefault(s => s.StudentId == student.Id);

                return new
                {
                    student = new
                    {
                        id = student.Id,
                        full_name = student.User.Name,
                        initials = initials
                    },
                    score = sub?.Score,
                    time_taken_seconds = sub != null && sub.CompletedAt.HasValue ? (int)(sub.CompletedAt.Value - sub.StartedAt).TotalSeconds : 0,
                    submitted_at = sub?.CompletedAt,
                    status = sub != null ? "completed" : "not_attempted"
                };
            })
            .OrderByDescending(sr => sr.status == "completed")
            .ThenByDescending(sr => sr.score ?? -1)
            .ThenBy(sr => sr.submitted_at ?? DateTimeOffset.MaxValue)
            .ToList();

            var completedSubmissions = studentResults.Where(sr => sr.status == "completed").ToList();
            int attemptsCount = completedSubmissions.Count;
            double avgScore = attemptsCount > 0 ? completedSubmissions.Average(s => s.score ?? 0) : 0.0;
            int highestScore = attemptsCount > 0 ? completedSubmissions.Max(s => s.score ?? 0) : 0;
            int lowestScore = attemptsCount > 0 ? completedSubmissions.Min(s => s.score ?? 0) : 0;

            var distribution = new List<object>();
            double bucketSize = totalPoints / 5.0;
            for (int i = 0; i < 5; i++)
            {
                int min = (int)Math.Round(i * bucketSize);
                int max = (i == 4) ? totalPoints : (int)Math.Round((i + 1) * bucketSize) - 1;
                if (max < min) max = min;
                
                var count = completedSubmissions.Count(s => (s.score ?? 0) >= min && (s.score ?? 0) <= max);
                distribution.Add(new
                {
                    range_label = $"{min}–{max}",
                    range_min = min,
                    range_max = max,
                    count = count
                });
            }

            var responseObj = new
            {
                quiz = new
                {
                    id = quiz.Id,
                    title = quiz.Title,
                    total_points = totalPoints,
                    attempts_count = attemptsCount,
                    average_score = avgScore,
                    highest_score = highestScore,
                    lowest_score = lowestScore
                },
                score_distribution = distribution,
                student_results = studentResults
            };

            return Ok(responseObj);
        }

        [HttpGet("quizzes/{quiz_id}/questions")]
        public async Task<IActionResult> GetQuestions(string quiz_id)
        {
            var userId = GetUserId();
            var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.UserId == userId);
            if (instructor == null) return Unauthorized();

            var quiz = await _context.Quizzes.FindAsync(quiz_id);
            if (quiz == null) return NotFound();

            var isTeaching = await _context.Sections.AnyAsync(s => s.CourseId == quiz.CourseId && s.InstructorId == instructor.Id);
            if (!isTeaching) return Forbid();

            var questions = await _context.QuizQuestions
                .Where(q => q.QuizId == quiz_id)
                .OrderBy(q => q.OrderIndex)
                .Select(q => new
                {
                    id = q.Id,
                    type = q.Type,
                    text = q.Text,
                    points = q.Points,
                    order = q.OrderIndex,
                    options = !string.IsNullOrEmpty(q.OptionsJson) ? JsonSerializer.Deserialize<List<string>>(q.OptionsJson, (JsonSerializerOptions)null!) : null,
                    correct_option_index = q.CorrectOptionIndex,
                    correct_boolean = q.CorrectBoolean
                })
                .ToListAsync();

            return Ok(new { questions = questions });
        }

        [HttpPost("quizzes/{quiz_id}/questions")]
        public async Task<IActionResult> AddQuestion(string quiz_id, [FromBody] QuestionRequest request)
        {
            var userId = GetUserId();
            var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.UserId == userId);
            if (instructor == null) return Unauthorized();

            var quiz = await _context.Quizzes.FindAsync(quiz_id);
            if (quiz == null) return NotFound();

            var isTeaching = await _context.Sections.AnyAsync(s => s.CourseId == quiz.CourseId && s.InstructorId == instructor.Id);
            if (!isTeaching) return Forbid();

            var question = new QuizQuestion
            {
                QuizId = quiz_id,
                Type = request.Type,
                Text = request.Text,
                Points = request.Points,
                OrderIndex = request.Order,
                OptionsJson = request.Options != null ? JsonSerializer.Serialize(request.Options) : null,
                CorrectOptionIndex = request.CorrectOptionIndex,
                CorrectBoolean = request.CorrectBoolean
            };

            _context.QuizQuestions.Add(question);
            await _context.SaveChangesAsync();

            var responseObj = new
            {
                question = new
                {
                    id = question.Id,
                    type = question.Type,
                    text = question.Text,
                    points = question.Points,
                    order = question.OrderIndex,
                    options = request.Options,
                    correct_option_index = question.CorrectOptionIndex,
                    correct_boolean = question.CorrectBoolean
                }
            };

            return Created("", responseObj);
        }
    }
}
