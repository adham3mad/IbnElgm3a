using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace IbnElgm3a.DTOs.Users
{
    public class BulkImportRequestDto
    {
        [Required]
        public IFormFile? File { get; set; }
    }
}
