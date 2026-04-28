using System;
using System.ComponentModel.DataAnnotations;

namespace IbnElgm3a.Models.Data
{
    public class Notification : BaseEntity
    {
        [Required]
        [StringLength(50)]
        public string Type { get; set; } = string.Empty; // announcement, exam, complaint, schedule, registration

        [Required]
        [StringLength(120)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;

        public bool IsRead { get; set; }
        public DateTimeOffset? ReadAt { get; set; }

        public string? ActionUrl { get; set; }

        [Required]
        [StringLength(50)]
        public string StudentId { get; set; } = string.Empty;
        public virtual Student? Student { get; set; }
    }
}
