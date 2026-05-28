using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IbnElgm3a.Models.Data
{
    [Table("room_attendance")]
    public class RoomAttendance : BaseEntity
    {
        [Required]
        public string StudentId { get; set; } = string.Empty;
        public virtual Student? Student { get; set; }

        [Required]
        public string RoomId { get; set; } = string.Empty;
        public virtual Room? Room { get; set; }

        [Required]
        [StringLength(64)]
        public string DeviceId { get; set; } = string.Empty;

        [Required]
        public DateTimeOffset ScannedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
