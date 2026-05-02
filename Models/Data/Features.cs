using IbnElgm3a.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace IbnElgm3a.Models.Data
{
    public class Complaint : BaseEntity
    {
        [Required]
        [StringLength(20)]
        public string TicketNumber { get; set; } = string.Empty;

        public string StudentId { get; set; } = string.Empty;
        public virtual User? Student { get; set; }

        public ComplaintType Type { get; set; }

        [Required]
        [StringLength(255)]
        public string Title { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;

        public ComplaintStatus Status { get; set; } = ComplaintStatus.Open;

        public string? AssignedToId { get; set; }
        public virtual User? AssignedTo { get; set; }

        public DateTimeOffset? LastResponseAt { get; set; }

        public string? Response { get; set; }

        public virtual ICollection<ComplaintNote> InternalNotes { get; set; } = new List<ComplaintNote>();
    }

    public class ComplaintNote : BaseEntity
    {
        public string ComplaintId { get; set; } = string.Empty;
        public virtual Complaint? Complaint { get; set; }

        public string AuthorId { get; set; } = string.Empty;
        public virtual User? Author { get; set; }

        [Required]
        public string Text { get; set; } = string.Empty;
    }

    public class SubAdmin : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public virtual User? User { get; set; }

        public SubAdminScopeType ScopeType { get; set; }
        public string? ScopeId { get; set; }
        public string? ScopeLabel { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTimeOffset? LastActiveAt { get; set; }
        
    }

    public class Announcement : BaseEntity
    {
        [Required]
        [StringLength(120)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;

        public AnnouncementTargetType TargetType { get; set; }
        
        [StringLength(100)]
        public string? TargetLabel { get; set; }

        public string? TargetId { get; set; }
        
        [StringLength(20)]
        public string? TargetRole { get; set; } // student, instructor, admin

        public int SentCount { get; set; }
        public int ReadCount { get; set; }

        public string CreatedById { get; set; } = string.Empty;
        public virtual User? CreatedBy { get; set; }

        public AnnouncementPriority Priority { get; set; } = AnnouncementPriority.Normal;

        // Instructor API specific properties
        public string? InstructorId { get; set; }
        public virtual Instructor? Instructor { get; set; }
        public string Audience { get; set; } = "all_students"; // all_students, specific_group

        public string? AttachmentUrl { get; set; }
        public string Status { get; set; } = "published"; // draft, published, scheduled
        public DateTimeOffset? ScheduledAt { get; set; }
        public bool SendPush { get; set; } = false;

        public virtual ICollection<AnnouncementCourse> AnnouncementCourses { get; set; } = new List<AnnouncementCourse>();
    }

    public class AnnouncementCourse : BaseEntity
    {
        [Required]
        public string AnnouncementId { get; set; } = string.Empty;
        public virtual Announcement? Announcement { get; set; }

        [Required]
        public string CourseId { get; set; } = string.Empty;
        public virtual Course? Course { get; set; }
    }

    public class CalendarEvent : BaseEntity
    {
        [Required]
        [StringLength(120)]
        public string Title { get; set; } = string.Empty;

        public DateTimeOffset Date { get; set; }
        public DateTimeOffset? EndDate { get; set; }

        public CalendarEventType Type { get; set; }

        public string? Description { get; set; }

        [StringLength(50)]
        public string SemesterId { get; set; } = string.Empty;

        public bool IsPublic { get; set; } = true;

        [StringLength(20)]
        public string ColorSeed { get; set; } = "blue";
    }

    public class UserDevice : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public virtual User? User { get; set; }

        [Required]
        [StringLength(100)]
        public string DeviceId { get; set; } = string.Empty;

        [StringLength(255)]
        public string? FcmToken { get; set; }

        [StringLength(500)]
        public string? BiometricPublicKey { get; set; }

        [StringLength(100)]
        public string? DeviceName { get; set; }

        [StringLength(20)]
        public string? DeviceOs { get; set; }
        
        public bool IsActive { get; set; } = true;
        public DateTimeOffset LastUsedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
