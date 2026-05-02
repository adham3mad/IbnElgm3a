using System;
using System.ComponentModel.DataAnnotations;

namespace IbnElgm3a.Models.Data
{
    public class AssignmentSubmission : BaseEntity
    {
        [Required]
        public string AssignmentId { get; set; } = string.Empty;
        public virtual Assignment? Assignment { get; set; }

        [Required]
        public string StudentId { get; set; } = string.Empty;
        public virtual Student? Student { get; set; }

        public string Status { get; set; } = "submitted"; // submitted, graded, returned, late

        public string? FileUrl { get; set; }

        public string? StudentComment { get; set; }

        public int? Score { get; set; }

        public string? Feedback { get; set; }

        public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
