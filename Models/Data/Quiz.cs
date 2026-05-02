using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IbnElgm3a.Models.Data
{
    public class Quiz : BaseEntity
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Status { get; set; } = "draft"; // draft, published, closed

        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }

        public int TimeLimitMinutes { get; set; } = 30;

        public bool ShuffleQuestions { get; set; } = true;

        [Required]
        public string CourseId { get; set; } = string.Empty;
        public virtual Course? Course { get; set; }

        public virtual ICollection<QuizQuestion> Questions { get; set; } = new List<QuizQuestion>();
    }
}
