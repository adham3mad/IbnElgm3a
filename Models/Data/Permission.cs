using IbnElgm3a.Enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IbnElgm3a.Models.Data
{
    public class Permission : IBasePermission
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public int? Method { get; set; }

        public PermissionEnum Code { get; set; }

        public string? Description { get; set; }
        
        [StringLength(100)]
        public string? Ar_Name { get; set; }
        
        public string? Ar_Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int FeatureId { get; set; }
        public virtual Feature Feature { get; set; } = null!;

        public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
    }
}
