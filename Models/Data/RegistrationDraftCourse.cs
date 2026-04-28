using System.ComponentModel.DataAnnotations;

namespace IbnElgm3a.Models.Data
{
    public class RegistrationDraftCourse : BaseEntity
    {
        [Required]
        [StringLength(50)]
        public string DraftId { get; set; } = string.Empty;
        public virtual RegistrationDraft? Draft { get; set; }

        [Required]
        [StringLength(50)]
        public string CourseId { get; set; } = string.Empty;
        public virtual Course? Course { get; set; }

        [Required]
        [StringLength(50)]
        public string SectionId { get; set; } = string.Empty;
        public virtual Section? Section { get; set; }
    }
}
