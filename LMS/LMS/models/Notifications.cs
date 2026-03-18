using System.ComponentModel.DataAnnotations;

namespace LMS.models
{
    public class Notifications
    {
        [Key]
        public int NotifcationId { get; set; }
        [Required]
        public int UserId { get; set; }
        [Required]
        public bool NotifcationDeleted { get; set; } = false;
    }
}
