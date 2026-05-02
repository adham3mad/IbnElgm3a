using IbnElgm3a.Enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IbnElgm3a.Models.Data
{
    public class User : BaseEntity
    {
        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty; // Masaar FullName

        [StringLength(255)]
        public string? FullNameAr { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        [StringLength(500)]
        public string? NationalId { get; set; }
        
        [StringLength(500)]
        public string? Phone { get; set; }

        public bool IsActive { get; set; } = true;
        
        public string RoleId { get; set; } = string.Empty;
        public virtual Role? Role { get; set; }
        public UserStatus Status { get; set; } = UserStatus.Active;
        
        [StringLength(500)]
        public string? AvatarUrl { get; set; }

        public string? FacultyId { get; set; }
        public virtual Faculty? Faculty { get; set; }

        public string? DepartmentId { get; set; }
        public virtual Department? Department { get; set; }

        public bool MustChangePw { get; set; }
        public bool ProfileComplete { get; set; }
        public string? InactiveReason { get; set; }
        public bool IsEmailConfirmed { get; internal set; }
        public DateTimeOffset? LastActiveAt { get; set; }

        // Navigation properties for split entities (optional for Masaar but keeping for existing logic)
        public virtual Student? Student { get; set; }
        public virtual Instructor? Instructor { get; set; }

        public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
        public virtual ICollection<UserDevice> Devices { get; set; } = new List<UserDevice>();
    }
}
