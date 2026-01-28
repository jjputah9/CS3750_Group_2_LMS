using System;
using System.ComponentModel.DataAnnotations;

namespace LMS.Models
{
    public class UserProfile
    {
        public int Id { get; set; }

        public string UserId { get; set; }

        [Required(ErrorMessage = "First Name is required")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last Name is required")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Birth Date is required")]
        [DataType(DataType.Date)]
        public DateTime BirthDate { get; set; }

        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }

        [Phone(ErrorMessage = "Invalid phone number")]
        public string Phone { get; set; }

        public string Link1 { get; set; }
        public string Link2 { get; set; }
        public string Link3 { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime LastUpdatedDate { get; set; }

        public UserProfile()
        {
            BirthDate = DateTime.Now.AddYears(-18);
            CreatedDate = DateTime.Now;
            LastUpdatedDate = DateTime.Now;
        }
    }
}