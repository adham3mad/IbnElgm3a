using System;
using System.ComponentModel.DataAnnotations;
using IbnElgm3a.Enums;

namespace IbnElgm3a.Models.Data
{
    public class Session : BaseEntity
    {
        [Required]
        public string SectionId { get; set; } = string.Empty;
        public virtual Section? Section { get; set; }

        public string? ScheduleSlotId { get; set; }
        public virtual ScheduleSlot? ScheduleSlot { get; set; }

        public int SessionNumber { get; set; }

        public ClassType Type { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        [StringLength(5)] // HH:mm
        public string StartTime { get; set; } = string.Empty;

        [Required]
        [StringLength(5)] // HH:mm
        public string EndTime { get; set; } = string.Empty;

        [StringLength(100)]
        public string RoomName { get; set; } = string.Empty;

        public int WeekNumber { get; set; }

        public string? Notes { get; set; }

        [StringLength(50)]
        public string AttendanceStatus { get; set; } = "pending"; // pending, in_progress, completed

        public bool IsRecurring { get; set; } = true;

        public string? QrToken { get; set; }
        public DateTimeOffset? QrExpiresAt { get; set; }
        public bool IsQrActive { get; set; }
    }

    public class AttendanceRecord : BaseEntity
    {
        [Required]
        public string SessionId { get; set; } = string.Empty;
        public virtual Session? Session { get; set; }

        [Required]
        public string StudentId { get; set; } = string.Empty;
        public virtual Student? Student { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "absent"; // present, late, absent, excused
    }
}
