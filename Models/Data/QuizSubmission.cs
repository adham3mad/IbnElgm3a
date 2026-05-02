using System;
using System.ComponentModel.DataAnnotations;

namespace IbnElgm3a.Models.Data
{
    public class QuizSubmission : BaseEntity
    {
        [Required]
        public string QuizId { get; set; } = string.Empty;
        public virtual Quiz? Quiz { get; set; }

        [Required]
        public string StudentId { get; set; } = string.Empty;
        public virtual Student? Student { get; set; }

        public int Score { get; set; } = 0;

        public DateTimeOffset StartedAt { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }

        public string Status { get; set; } = "in_progress"; // in_progress, completed, abandoned
    }
}
