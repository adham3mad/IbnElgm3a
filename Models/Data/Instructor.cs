using System;

namespace IbnElgm3a.Models.Data
{
    public class Instructor : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public virtual User? User { get; set; }

        public string? Rank { get; set; }
        public string? OfficeHours { get; set; }
        public string? Bio { get; set; }

        public string DepartmentId { get; set; } = string.Empty;
        public virtual Department? Department { get; set; }
    }
}
