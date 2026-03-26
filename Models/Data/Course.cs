using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IbnElgm3a.Model.Data
{
    public class Course : BaseEntity
    {
        [StringLength(20)]
        public string CourseCode { get; set; } = string.Empty; // Masaar Code

        [Required]
        [StringLength(255)]
        public string Title { get; set; } = string.Empty; // Masaar Name
        
        [StringLength(255)]
        public string? TitleAr { get; set; } // Masaar NameAr

        public string? Description { get; set; }
        public string? Syllabus { get; set; }
        public int CreditHours { get; set; }

        public string DepartmentId { get; set; } = string.Empty;
        public virtual Department? Department { get; set; }
        
        public string? SemesterId { get; set; }
        public virtual Semester? Semester { get; set; }

        public string? InstructorId { get; set; }
        public virtual Instructor? Instructor { get; set; }

        public int MaxStudents { get; set; }
        public int EnrolledCount { get; set; }

        public virtual ICollection<Section> Sections { get; set; } = new List<Section>();
    }
}
