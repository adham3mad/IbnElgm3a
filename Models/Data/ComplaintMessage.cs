using System;
using System.ComponentModel.DataAnnotations;

namespace IbnElgm3a.Models.Data
{
    public class ComplaintMessage : BaseEntity
    {
        public string ComplaintId { get; set; } = string.Empty;
        public virtual Complaint? Complaint { get; set; }

        public string SenderId { get; set; } = string.Empty;
        public virtual User? Sender { get; set; }

        [Required]
        [StringLength(20)]
        public string SenderRole { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        // Simplified attachments handling for the model
        // In a full implementation this would be a separate entity `ComplaintAttachment`
        public string? AttachmentsJson { get; set; } 
    }
}
