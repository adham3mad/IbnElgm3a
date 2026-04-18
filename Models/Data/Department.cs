using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IbnElgm3a.Models.Data
{
    public class Department : BaseEntity
    {
        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        [StringLength(255)]
        public string? NameAr { get; set; }
        
        public string Code { get; set; } = string.Empty;
        
        [Required]
        [StringLength(20)]
        public string DepCode { get; set; } = string.Empty;

        public string FacultyId { get; set; } = string.Empty;
        public virtual Faculty? Faculty { get; set; }

        public int LevelCount { get; set; } = 4;
        public IbnElgm3a.Enums.UserStatus Status { get; set; } = IbnElgm3a.Enums.UserStatus.Active;

        public string? HeadUserId { get; set; }
        public virtual User? Head { get; set; }

        public int StudentCount { get; set; }
        public int InstructorCount { get; set; }
        public int CourseCount { get; set; }

        [StringLength(20)]
        public string? AccentColor { get; set; }

        public virtual ICollection<Student> Students { get; set; } = new List<Student>();
        public virtual ICollection<Instructor> Instructors { get; set; } = new List<Instructor>();
    }
}
