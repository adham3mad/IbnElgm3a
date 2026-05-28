using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IbnElgm3a.Models.Data
{
    [Table("campus_sessions")]
    public class CampusSession : BaseEntity
    {
        [Required]
        public string StudentId { get; set; } = string.Empty;
        public virtual Student? Student { get; set; }

        [Required]
        [StringLength(64)]
        public string DeviceId { get; set; } = string.Empty;

        [Required]
        public DateTimeOffset EntryTime { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? ExitTime { get; set; } // null = currently on campus

        // Configured in DbContext as computed column (exit_time - entry_time)
        public TimeSpan? Duration { get; set; }
    }
}
