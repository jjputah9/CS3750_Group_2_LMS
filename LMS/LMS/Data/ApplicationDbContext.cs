using LMS.models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using LMS.Models;

namespace LMS.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // Existing DbSet
        public DbSet<LMS.Models.Course> Course { get; set; } = default!;

        // Registration DbSet
        public DbSet<Registration> Registration { get; set; } = default!;

        // Add UserProfiles DbSet - make sure it matches your namespace
        public DbSet<UserProfile> UserProfiles { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure UserProfiles table
            modelBuilder.Entity<UserProfile>(entity =>
            {
                entity.ToTable("UserProfiles"); // Matches your database table
                entity.HasKey(e => e.Id);

                // Configure properties to match your database schema
                entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(50);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(50);
                entity.Property(e => e.AddressLine1).HasMaxLength(100);
                entity.Property(e => e.AddressLine2).HasMaxLength(100);
                entity.Property(e => e.City).HasMaxLength(50);
                entity.Property(e => e.State).HasMaxLength(50);
                entity.Property(e => e.ZipCode).HasMaxLength(10);
                entity.Property(e => e.Phone).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Link1).HasMaxLength(200);
                entity.Property(e => e.Link2).HasMaxLength(200);
                entity.Property(e => e.Link3).HasMaxLength(200);

                // Add index for better performance
                entity.HasIndex(e => e.UserId);
            });
        }
    }
}