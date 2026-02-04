using LMS.Models;
using System.ComponentModel.DataAnnotations;

namespace LMS.models
{
    public class Assignment
    {
        [Key]
        public int AssignmentId { get; set; }
        [Required]
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        [Required, Range(0, int.MaxValue, ErrorMessage = "Points must be zero or greater.")]
        public int Points { get; set; }
        [Required]
        public DateTime DueDate { get; set; }

        [Required]
        public int CourseId { get; set; }
        [Required]
        public int SubmissionTypeId { get; set; }
        // Navigation property
        public Course? Course { get; set; }
        public SubmissionType? SubmissionType { get; set; }
    }
}
