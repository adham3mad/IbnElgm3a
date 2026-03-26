using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace IbnElgm3a.Model.Data
{
    public class Guardian : BaseEntity
    {
        public string FullName { get; set; } = string.Empty;
        [StringLength(500)]
        public string NationalId { get; set; } = string.Empty;

        [StringLength(500)]
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Job { get; set; } = string.Empty;

        public virtual ICollection<StudentGuardian> Students { get; set; } = new List<StudentGuardian>();
    }
}
