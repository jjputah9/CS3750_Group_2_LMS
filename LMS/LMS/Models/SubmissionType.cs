using LMS.Models;
using System.ComponentModel.DataAnnotations;

namespace LMS.models
{
    public class SubmissionType
    {
        [Key]
        public int SubmissionTypeId { get; set; }
        [Required]
        public string TypeName { get; set; }

    }
}
