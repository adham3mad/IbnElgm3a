using System;
using System.ComponentModel.DataAnnotations;

namespace IbnElgm3a.Models.Data
{
    public class QuizQuestion : BaseEntity
    {
        [Required]
        public string QuizId { get; set; } = string.Empty;
        public virtual Quiz? Quiz { get; set; }

        [Required]
        [StringLength(50)]
        public string Type { get; set; } = "mcq"; // mcq, true_false, short_answer

        [Required]
        public string Text { get; set; } = string.Empty;

        public int Points { get; set; } = 1;

        public int OrderIndex { get; set; } = 0;

        // Store options as a JSON string or delimited
        public string? OptionsJson { get; set; }

        public int? CorrectOptionIndex { get; set; }

        public bool? CorrectBoolean { get; set; }
    }
}
