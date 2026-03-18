using LMS.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LMS.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Course> Course { get; set; } = default!;
        public DbSet<Registration> Registration { get; set; } = default!;
        public DbSet<Payment> Payments { get; set; } = default!;
        public DbSet<UserProfile> UserProfiles { get; set; } = default!;
        public DbSet<Assignment> Assignment { get; set; } = default!;
        public DbSet<submittedAssignment> submittedAssignments { get; set; } = default!;
        public DbSet<Notifications> Notifications { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserProfile>(entity =>
            {
                entity.ToTable("UserProfiles");
                entity.HasKey(e => e.Id);

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

                entity.HasIndex(e => e.UserId);
            });
        }
    }
}