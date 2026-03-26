using IbnElgm3a.Enums;
using System;
using System.Collections.Generic;

namespace IbnElgm3a.Model.Data
{
    public class Student : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public virtual User? User { get; set; }

        public string AcademicNumber { get; set; } = string.Empty; // Masaar StudentId
        public DateTimeOffset EnrollmentDate { get; set; }
        public int Level { get; set; } // Masaar Year
        public decimal GPA { get; set; }
        public bool IsActive { get; set; }

        public DateTimeOffset BirthDate { get; set; }
        public Gender Gender { get; set; }
        public string? Nationality { get; set; }

        public string DepartmentId { get; set; } = string.Empty;
        public virtual Department? Department { get; set; }

        public virtual ICollection<StudentGuardian> Guardians { get; set; } = new List<StudentGuardian>();
        public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    }
}
