using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LMS.models  // Note: lowercase 'models' to match your using statement
{
    public class UserProfile
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime? BirthDate { get; set; }

        [StringLength(100)]
        public string? AddressLine1 { get; set; }

        [StringLength(100)]
        public string? AddressLine2 { get; set; }

        [StringLength(50)]
        public string? City { get; set; }

        [StringLength(50)]
        public string? State { get; set; }

        [StringLength(10)]
        public string? ZipCode { get; set; }

        [Required]
        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Link1 { get; set; }

        [StringLength(200)]
        public string? Link2 { get; set; }

        [StringLength(200)]
        public string? Link3 { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";
    }
}