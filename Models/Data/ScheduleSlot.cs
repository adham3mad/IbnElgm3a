using IbnElgm3a.Enums;
using System.ComponentModel.DataAnnotations;

namespace IbnElgm3a.Model.Data
{
    public class Room : BaseEntity
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public int Capacity { get; set; }

        public string? FacultyId { get; set; }
        public virtual Faculty? Faculty { get; set; }
    }

    public class ScheduleSlot : BaseEntity
    {
        public string CourseSectionId { get; set; } = string.Empty;
        public virtual Section? CourseSection { get; set; }

        public string RoomId { get; set; } = string.Empty;
        public virtual Room? Room { get; set; }

        public DayOfWeekEnum Day { get; set; }

        [Required]
        [StringLength(5)] // HH:mm
        public string StartTime { get; set; } = string.Empty;

        [Required]
        [StringLength(5)] // HH:mm
        public string EndTime { get; set; } = string.Empty;

        public ClassType Type { get; set; }

        public ScheduleRecurrence Recurrence { get; set; } = ScheduleRecurrence.Weekly;

        [StringLength(50)]
        public string SemesterId { get; set; } = string.Empty;
    }
}
