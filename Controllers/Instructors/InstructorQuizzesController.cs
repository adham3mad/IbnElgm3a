using IbnElgm3a.Models;
using IbnElgm3a.Models.Data;
using IbnElgm3a.Services;
using IbnElgm3a.Services.Localization;
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
        public async Task<IActionResult> GetQuizzes(string course_id)
        {
            var userId = GetUserId();
            var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.UserId == userId);
            if (instructor == null) return Unauthorized();

            var isTeaching = await _context.Sections.AnyAsync(s => s.CourseId == course_id && s.InstructorId == instructor.Id);
            if (!isTeaching) return Forbid();

            var quizzes = await _context.Quizzes
                .Where(q => q.CourseId == course_id)
                .OrderByDescending(q => q.CreatedAt)
                .Select(q => new
                {
                    id = q.Id,
                    title = q.Title,
                    status = q.Status,
                    question_count = q.Questions.Count,
                    start_date = q.StartDate,
                    end_date = q.EndDate,
                    time_limit = q.TimeLimitMinutes
                })
                .ToListAsync();

            return Ok(new { data = quizzes });
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
                Description = request.Description,
                Status = "draft",
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                TimeLimitMinutes = request.TimeLimitMinutes,
                ShuffleQuestions = request.ShuffleQuestions
            };

            _context.Quizzes.Add(quiz);
            await _context.SaveChangesAsync();

            return Created("", new { data = quiz });
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
                .ToListAsync();

            return Ok(new { data = questions });
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
                OrderIndex = request.OrderIndex,
                OptionsJson = request.Options != null ? JsonSerializer.Serialize(request.Options) : null,
                CorrectOptionIndex = request.CorrectOptionIndex,
                CorrectBoolean = request.CorrectBoolean
            };

            _context.QuizQuestions.Add(question);
            await _context.SaveChangesAsync();

            return Created("", new { data = question });
        }


    }
}
