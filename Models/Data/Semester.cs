using System;
using System.Collections.Generic;

namespace IbnElgm3a.Models.Data
{
    public class Semester : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? NameAr { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
        public DateTimeOffset? RegistrationStartDate { get; set; }
        public DateTimeOffset? RegistrationEndDate { get; set; }
        public bool IsActive { get; set; }
        
        public int CurrentWeek { get; set; }
        public int TotalWeeks { get; set; }
        public string? NextEvent { get; set; }

        public virtual ICollection<Section> Sections { get; set; } = new List<Section>();
        public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
    }
}
