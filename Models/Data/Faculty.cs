using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IbnElgm3a.Model.Data
{
    public class Faculty : BaseEntity
    {
        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        [StringLength(255)]
        public string? NameAr { get; set; }

        [Required]
        [StringLength(10)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string FacCode { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Building { get; set; }

        [StringLength(255)]
        public string? OfficialEmail { get; set; }

        [StringLength(20)]
        public string? OfficialPhone { get; set; }

        public int EstablishedYear { get; set; }

        // Settings from §3.13.1
        public bool AcceptAdmissions { get; set; } = true;
        public bool PublicProfile { get; set; } = true;
        public bool AiChatbotEnabled { get; set; } = true;

        public int StudentCount { get; set; }
        public int InstructorCount { get; set; }

        public string? HeadOfFacultyId { get; set; }
        public virtual User? HeadOfFaculty { get; set; }

        public virtual ICollection<Department> Departments { get; set; } = new List<Department>();
    }
}
