using System;
using System.ComponentModel.DataAnnotations;

namespace IbnElgm3a.Model.Data
{
    public class BulkImportJob : BaseEntity
    {
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "queued"; // queued, processing, done, failed
        
        public int Total { get; set; } = 0;
        public int Imported { get; set; } = 0;
        
        public string FailedRowsJson { get; set; } = "[]";
    }
}
