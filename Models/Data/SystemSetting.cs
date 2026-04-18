using System;
using System.ComponentModel.DataAnnotations;

namespace IbnElgm3a.Models.Data
{
    public class SystemSetting
    {
        [Key]
        [MaxLength(100)]
        public string Key { get; set; } = null!;
        public string ValueJson { get; set; } = string.Empty;
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
