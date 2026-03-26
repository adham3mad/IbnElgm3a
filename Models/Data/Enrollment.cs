using IbnElgm3a.Enums;
using System;

namespace IbnElgm3a.Model.Data
{
    public class Enrollment : BaseEntity
    {
        public string StudentId { get; set; } = string.Empty;
        public virtual Student? Student { get; set; }

        public string SectionId { get; set; } = string.Empty;
        public virtual Section? Section { get; set; }

        public EnrollmentStatus Status { get; set; }
        public DateTimeOffset EnrolledAt { get; set; }

        public virtual Grade? Grade { get; set; }
    }
}
