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
}
