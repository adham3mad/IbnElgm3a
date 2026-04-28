using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IbnElgm3a.Models.Data
{
    public class RegistrationRequest : BaseEntity
    {
        [Required]
        [StringLength(50)]
        public string StudentId { get; set; } = string.Empty;
        public virtual Student? Student { get; set; }

        [Required]
        [StringLength(50)]
        public string SemesterId { get; set; } = string.Empty;
        public virtual Semester? Semester { get; set; }

        [Required]
        [StringLength(50)]
        public string RefCode { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "pending"; // pending, approved, rejected, cancelled

        public DateTimeOffset? SubmittedAt { get; set; }
        public DateTimeOffset? ReviewedAt { get; set; }
        public string? ReviewerNote { get; set; }

        public virtual ICollection<RegistrationRequestCourse> Courses { get; set; } = new List<RegistrationRequestCourse>();
    }
}
