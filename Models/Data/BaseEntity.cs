using System;
using System.ComponentModel.DataAnnotations;

namespace IbnElgm3a.Models.Data
{
    public abstract class BaseEntity
    {
        [Key]
        [StringLength(50)]
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
