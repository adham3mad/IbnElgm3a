using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace IbnElgm3a.DTOs.Announcements
{
    public class AnnouncementRequest
    {
        [Required]
        [StringLength(120)]
        public string Title { get; set; } = null!;

        [Required]
        public string Body { get; set; } = null!;

        [Required]
        public List<string> CourseIds { get; set; } = new();

        public string Audience { get; set; } = "all_students";
        public bool SendPush { get; set; }
        public string? AttachmentUrl { get; set; }
        public IFormFile? File { get; set; }
        public string Status { get; set; } = "published";
        public DateTimeOffset? ScheduledAt { get; set; }
    }

    public class UpdateAnnouncementRequest
    {
        [StringLength(120)]
        public string? Title { get; set; }

        public string? Body { get; set; }

        public List<string>? CourseIds { get; set; }

        public string? Audience { get; set; }
        public bool? SendPush { get; set; }
        public string? Status { get; set; }
        public DateTimeOffset? ScheduledAt { get; set; }
        public IFormFile? File { get; set; }
    }
}
