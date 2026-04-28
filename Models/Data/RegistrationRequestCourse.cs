using System.ComponentModel.DataAnnotations;

namespace IbnElgm3a.Models.Data
{
    public class RegistrationRequestCourse : BaseEntity
    {
        [Required]
        [StringLength(50)]
        public string RequestId { get; set; } = string.Empty;
        public virtual RegistrationRequest? Request { get; set; }

        [Required]
        [StringLength(50)]
        public string CourseId { get; set; } = string.Empty;
        public virtual Course? Course { get; set; }

        [Required]
        [StringLength(50)]
        public string SectionId { get; set; } = string.Empty;
        public virtual Section? Section { get; set; }

        [Required]
        [StringLength(20)]
        public string ApprovalStatus { get; set; } = "pending"; // pending, approved, rejected
    }
}
