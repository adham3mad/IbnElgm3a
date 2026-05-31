using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IbnElgm3a.DTOs.Academics
{
    public class AttendanceUpdateItem
    {
        [Required]
        public string StudentId { get; set; } = null!;

        [Required]
        [RegularExpression("present|late|absent|excused")]
        public string Status { get; set; } = null!;
    }

    public class AttendanceUpdateRequest
    {
        [Required]
        public List<AttendanceUpdateItem> Records { get; set; } = new();

        [Required]
        [RegularExpression("in_progress|completed")]
        public string Status { get; set; } = "completed";
    }
}
