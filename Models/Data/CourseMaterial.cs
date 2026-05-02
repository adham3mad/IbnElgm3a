using System;
using System.ComponentModel.DataAnnotations;

namespace IbnElgm3a.Models.Data
{
    public class CourseMaterial : BaseEntity
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Type { get; set; } = "pdf"; // pdf, video, link, other

        public string? FileUrl { get; set; }
        public string? ExternalUrl { get; set; }

        public long? FileSizeBytes { get; set; }
        public int? DurationSeconds { get; set; }

        public string Status { get; set; } = "draft"; // draft, published
        public int ViewCount { get; set; } = 0;

        public int WeekNumber { get; set; }

        [Required]
        public string CourseId { get; set; } = string.Empty;
        public virtual Course? Course { get; set; }
    }
}
