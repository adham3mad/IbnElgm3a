using IbnElgm3a.Enums;
using System;

namespace IbnElgm3a.Model.Data
{
    public class Grade : BaseEntity
    {
        public string EnrollmentId { get; set; } = string.Empty;
        public virtual Enrollment? Enrollment { get; set; }

        public decimal Marks { get; set; }
        public LetterGrade LetterGrade { get; set; }
        public string? Remarks { get; set; }

        public DateTimeOffset LastUpdated { get; set; }
    }
}
