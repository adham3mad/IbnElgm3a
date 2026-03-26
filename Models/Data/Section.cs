using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IbnElgm3a.Model.Data
{
    public class Section : BaseEntity
    {
        public string CourseId { get; set; } = string.Empty;
        public virtual Course? Course { get; set; }

        public string SemesterId { get; set; } = string.Empty;
        public virtual Semester? Semester { get; set; }

        public string? InstructorId { get; set; }
        public virtual Instructor? Instructor { get; set; }

        [StringLength(100)]
        public string Name { get; set; } = string.Empty; // Masaar Section Name

        public int Capacity { get; set; }
        public string? DayOfWeek { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string? Room { get; set; }

        public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    }
}
