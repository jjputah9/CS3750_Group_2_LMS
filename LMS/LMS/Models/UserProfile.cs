using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LMS.Models
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

        // ========== ADD THESE PROFILE PICTURE PROPERTIES ==========
        [StringLength(500)]
        public string? ProfilePictureUrl { get; set; }

        public byte[]? ProfilePictureData { get; set; }

        [StringLength(100)]
        public string? ProfilePictureContentType { get; set; }

        [StringLength(255)]
        public string? ProfilePictureFileName { get; set; }

        public DateTime? ProfilePictureUploadedAt { get; set; }
        // ========== END OF PROFILE PICTURE PROPERTIES ==========

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";

        [NotMapped]
        public string Initials =>
            !string.IsNullOrEmpty(FirstName) && !string.IsNullOrEmpty(LastName)
                ? $"{FirstName[0]}{LastName[0]}".ToUpper()
                : "??";
    }
}