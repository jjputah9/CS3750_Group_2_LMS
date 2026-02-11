using LMS.Models;
using System.ComponentModel.DataAnnotations;

namespace LMS.Models
{
    public class SubmissionType
    {
        [Key]
        public int SubmissionTypeId { get; set; }
        [Required]
        public string TypeName { get; set; }

    }
}
