using System;
using System.ComponentModel.DataAnnotations;

namespace LMS.Models
{
    public class Payment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string StudentId { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public string PaymentType { get; set; } // "full_tuition" or "partial_tuition"

        public string StripeSessionId { get; set; }

        public DateTime PaymentDate { get; set; }

        [Required]
        public string Status { get; set; } // "Completed", "Pending", "Failed"

        public string? Notes { get; set; }
    }
}