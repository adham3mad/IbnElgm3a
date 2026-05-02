using IbnElgm3a.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace IbnElgm3a.DTOs.Schedules
{
    public class CreateSessionRequest
    {
        [Required]
        public string SectionId { get; set; } = null!;

        [Required]
        public ClassType Type { get; set; }

        [Required]
        [RegularExpression(@"\d{4}-\d{2}-\d{2}")]
        public string Date { get; set; } = null!;

        [Required]
        [RegularExpression(@"\d{2}:\d{2}")]
        public string StartTime { get; set; } = null!;

        [Required]
        [RegularExpression(@"\d{2}:\d{2}")]
        public string EndTime { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string Room { get; set; } = null!;

        public string? Notes { get; set; }
        public int SessionNumber { get; set; }
        public int WeekNumber { get; set; }
    }
}
