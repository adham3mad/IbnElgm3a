using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IbnElgm3a.Models.Data
{
    [Table("cards")]
    public class Card : BaseEntity
    {
        [Required]
        [StringLength(32)]
        public string Uid { get; set; } = string.Empty; // "A1:B2:C3:D4"

        public string? StudentId { get; set; }
        public virtual Student? Student { get; set; }

        [Required]
        [StringLength(16)]
        public string Status { get; set; } = "active"; // "active", "inactive", "pending"

        [Required]
        public DateTimeOffset EnrolledAt { get; set; } = DateTimeOffset.UtcNow;

        [StringLength(64)]
        public string? EnrolledBy { get; set; } // device_id of enrolling unit

        public string? Notes { get; set; }
    }
}
