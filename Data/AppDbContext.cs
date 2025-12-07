using Microsoft.EntityFrameworkCore;
using MVC_project.Models;

namespace MVC_project.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Trip> Trips { get; set; }
        public DbSet<TripImage> TripImages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Map the User entity to the dbo.Users table
            modelBuilder.Entity<User>()
                .ToTable("Users", "dbo");

            // Set 'email' as the primary key of the User entity
            modelBuilder.Entity<User>()
                .HasKey(u => u.email);

            // Configure Trip entity
            modelBuilder.Entity<Trip>()
                .ToTable("Trips", "dbo");

            modelBuilder.Entity<Trip>()
                .HasKey(t => t.TripID);

            // Configure TripImage entity
            modelBuilder.Entity<TripImage>()
                .ToTable("TripImages", "dbo");

            modelBuilder.Entity<TripImage>()
                .HasKey(ti => ti.ImageID);

            // Configure relationship: Trip -> TripImages (One-to-Many)
            modelBuilder.Entity<TripImage>()
                .HasOne(ti => ti.Trip)
                .WithMany()
                .HasForeignKey(ti => ti.TripID)
                .OnDelete(DeleteBehavior.Cascade);

            base.OnModelCreating(modelBuilder);
        }
    }
}
