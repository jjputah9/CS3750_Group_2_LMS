using LMS.Models;
using System.ComponentModel.DataAnnotations;

namespace LMS.Models
{
    public class submittedAssignment
    {
        [Key]
        public int submittedAssignmentId { get; set; }

        [Required]
        public int AssignmentId { get; set; }

        [Required]
        public string StudentId { get; set; } = string.Empty; // Changed from int to string

        [Required]
        public int submissionTypeId { get; set; }

        public SubmissionType SubmissionType { get; set; }

        public string filePath { get; set; } = string.Empty;

        public DateTime submissionDate { get; set; }

        public string textSubmission { get; set; } = string.Empty;

        public int grade { get; set; }
    }
}
