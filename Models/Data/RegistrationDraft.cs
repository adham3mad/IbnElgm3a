using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IbnElgm3a.Models.Data
{
    public class RegistrationDraft : BaseEntity
    {
        [Required]
        [StringLength(50)]
        public string StudentId { get; set; } = string.Empty;
        public virtual Student? Student { get; set; }

        [Required]
        [StringLength(50)]
        public string SemesterId { get; set; } = string.Empty;
        public virtual Semester? Semester { get; set; }

        public virtual ICollection<RegistrationDraftCourse> Courses { get; set; } = new List<RegistrationDraftCourse>();
    }
}
