using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IbnElgm3a.Models.Data
{
    [Table("scan_audits")]
    public class ScanAudit : BaseEntity
    {
        [Required]
        [StringLength(32)]
        public string Uid { get; set; } = string.Empty;

        [Required]
        [StringLength(64)]
        public string DeviceId { get; set; } = string.Empty;

        [Required]
        [StringLength(64)]
        public string Endpoint { get; set; } = string.Empty; // e.g. "/attendance/room"

        [Required]
        public int HttpStatus { get; set; }

        [Required]
        [StringLength(32)]
        public string Result { get; set; } = string.Empty; // "granted", "denied", "error"

        [Required]
        public DateTimeOffset ScannedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
