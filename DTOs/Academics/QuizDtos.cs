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

        [Required]
        public string Description { get; set; } = null!;

        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public int TimeLimitMinutes { get; set; }
        public bool ShuffleQuestions { get; set; }
    }

    public class QuestionRequest
    {
        [Required]
        [RegularExpression("multiple_choice|true_false|short_answer")]
        public string Type { get; set; } = null!;

        [Required]
        public string Text { get; set; } = null!;

        [Range(1, 100)]
        public int Points { get; set; }

        public int OrderIndex { get; set; }
        public List<string>? Options { get; set; }
        public int? CorrectOptionIndex { get; set; }
        public bool? CorrectBoolean { get; set; }
    }
}
