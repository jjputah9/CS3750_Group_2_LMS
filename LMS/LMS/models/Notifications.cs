using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LMS.Models
{
    public class Notifications
    {
        [Key]
        public int NotificationId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId))]
        public ApplicationUser? User { get; set; }

        [StringLength(50)]
        public string? NotificationType { get; set; }

        public int? AssignmentId { get; set; }

        [ForeignKey(nameof(AssignmentId))]
        public Assignment? Assignment { get; set; }

        public int? SubmittedAssignmentId { get; set; }

        [ForeignKey(nameof(SubmittedAssignmentId))]
        public submittedAssignment? SubmittedAssignment { get; set; }

        public string? Message { get; set; }

        [Required]
        public bool NotificationDeleted { get; set; } = false;

        public bool IsRead { get; set; } = false;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}