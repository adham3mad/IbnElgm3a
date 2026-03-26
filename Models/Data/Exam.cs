using IbnElgm3a.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IbnElgm3a.Model.Data
{
    public class Exam : BaseEntity
    {
        public string CourseId { get; set; } = string.Empty;
        public virtual Course? Course { get; set; }

        [StringLength(50)]
        public string SemesterId { get; set; } = string.Empty;

        public ExamType Type { get; set; }

        public DateTimeOffset Date { get; set; }

        [StringLength(5)] // HH:mm
        public string StartTime { get; set; } = string.Empty;

        public int DurationMinutes { get; set; }

        public string HallId { get; set; } = string.Empty;
        public virtual Room? Hall { get; set; } // Map Room as Hall

        public ExamStatus Status { get; set; } = ExamStatus.Draft;
        
        public bool HasSeatPlan { get; set; }
        
        [StringLength(500)]
        public string? LayoutUrl { get; set; }
        
        public int EnrolledCount { get; set; }

        public virtual ICollection<ExamInvigilator> Invigilators { get; set; } = new List<ExamInvigilator>();
    }

    public class ExamInvigilator : BaseEntity
    {
        public string ExamId { get; set; } = string.Empty;
        public virtual Exam? Exam { get; set; }

        public string UserId { get; set; } = string.Empty;
        public virtual User? User { get; set; } // Must be Instructor role
    }
}
