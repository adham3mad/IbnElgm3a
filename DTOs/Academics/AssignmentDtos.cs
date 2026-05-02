using System;
using System.ComponentModel.DataAnnotations;

namespace IbnElgm3a.DTOs.Academics
{
    public class AssignmentRequest
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = null!;

        [Required]
        public string Description { get; set; } = null!;

        [Required]
        public DateTime DueDate { get; set; }

        [Range(1, 1000)]
        public int MaxPoints { get; set; }

        public bool AllowLateSubmissions { get; set; }
        public string? AttachmentUrl { get; set; }
    }

    public class GradeRequest
    {
        [Range(0, 1000)]
        public int Score { get; set; }
        public string? Feedback { get; set; }
    }
}
