using IbnElgm3a.Enums;
using IbnElgm3a.Models.Data;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IbnElgm3a.Models.Data
{
    public class Role : BaseEntity
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        public string? NameAr { get; set; }

        public string? Description { get; set; }

        public string? DescriptionAr { get; set; }

        public AppType Type { get; set; }

        public bool IsActive { get; set; } = true;

        public virtual ICollection<Permission> Permissions { get; set; } = new List<Permission>();
        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }
}
