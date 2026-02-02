using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace LMS.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        public string? fName { get; set; }
        [Required]
        public string? lName { get; set; }
        [Required]
        public DateTime DOB { get; set; }
        [Required]
        public string? UserType { get; set; }
    }
}