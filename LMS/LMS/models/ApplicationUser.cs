using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace LMS.models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        public string? fName { get; set; }
        [Required]
        public string? lName { get; set; }
        [Required]
        public DateTime DOB { get; set; }
    }
}