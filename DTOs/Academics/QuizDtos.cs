using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IbnElgm3a.DTOs.Academics
{
    public class QuizRequest
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = null!;
        public string Description { get; set; } = string.Empty;

        [Required]
        public int TimeLimitMinutes { get; set; }

        public int AttemptsAllowed { get; set; } = 1;

        public DateTimeOffset? OpensAt { get; set; }

        public DateTimeOffset? ClosesAt { get; set; }

        public bool ShuffleQuestions { get; set; }

        [Required]
        [RegularExpression("draft|published|closed")]
        public string Status { get; set; } = "draft";

        public List<QuestionRequest> Questions { get; set; } = new();
    }

    public class QuestionRequest
    {
        public string? Id { get; set; }

        [Required]
        [RegularExpression("mcq|true_false|short_answer")]
        public string Type { get; set; } = null!;

        [Required]
        public string Text { get; set; } = null!;

        [Range(1, 1000)]
        public int Points { get; set; }

        public int Order { get; set; }

        public List<string>? Options { get; set; }

        public int? CorrectOptionIndex { get; set; }

        public bool? CorrectBoolean { get; set; }
    }
}
