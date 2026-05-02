using System;
using System.ComponentModel.DataAnnotations;

namespace IbnElgm3a.Models.Data
{
    public class Assignment : BaseEntity
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateTime DueDate { get; set; }

        public int MaxPoints { get; set; } = 100;

        public string Status { get; set; } = "draft"; // draft, published, closed
        public bool GradesPublished { get; set; } = false;

        public bool AllowLateSubmissions { get; set; } = false;

        public string? AttachmentUrl { get; set; }

        [Required]
        public string CourseId { get; set; } = string.Empty;
        public virtual Course? Course { get; set; }
    }
}
