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
        public DbSet<UserTrip> UserTrips { get; set; }
        public DbSet<TripDate> TripDates { get; set; }
        public DbSet<Waitlist> Waitlist { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Map the User entity to the dbo.Users table
            modelBuilder.Entity<User>()
                .ToTable("Users", "dbo");

            // Set 'Id' as the primary key of the User entity
            modelBuilder.Entity<User>()
                .HasKey(u => u.Id);

            // Email should be unique
            modelBuilder.Entity<User>()
                .HasIndex(u => u.email)
                .IsUnique()
                .HasDatabaseName("UQ_Users_Email");

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

            // Configure UserTrip entity
            modelBuilder.Entity<UserTrip>()
                .ToTable("UserTrips", "dbo");

            modelBuilder.Entity<UserTrip>()
                .HasKey(ut => ut.UserTripID);

            // Unique constraint on UserId, TripID, and SelectedDateIndex
            modelBuilder.Entity<UserTrip>()
                .HasIndex(ut => new { ut.UserId, ut.TripID, ut.SelectedDateIndex })
                .IsUnique()
                .HasDatabaseName("UQ_UserTrips_User_Trip_Date");

            // Configure relationships
            modelBuilder.Entity<UserTrip>()
                .HasOne(ut => ut.User)
                .WithMany()
                .HasForeignKey(ut => ut.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserTrip>()
                .HasOne(ut => ut.Trip)
                .WithMany()
                .HasForeignKey(ut => ut.TripID)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure TripDate entity
            modelBuilder.Entity<TripDate>()
                .ToTable("TripDates", "dbo");

            modelBuilder.Entity<TripDate>()
                .HasKey(td => td.TripDateID);

            // Configure relationship: Trip -> TripDates (One-to-Many)
            modelBuilder.Entity<TripDate>()
                .HasOne(td => td.Trip)
                .WithMany()
                .HasForeignKey(td => td.TripID)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Waitlist entity
            modelBuilder.Entity<Waitlist>()
                .ToTable("Waitlist", "dbo");

            modelBuilder.Entity<Waitlist>()
                .HasKey(w => w.WaitlistID);

            // Configure relationships for Waitlist
            modelBuilder.Entity<Waitlist>()
                .HasOne(w => w.User)
                .WithMany()
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Waitlist>()
                .HasOne(w => w.Trip)
                .WithMany()
                .HasForeignKey(w => w.TripId)
                .OnDelete(DeleteBehavior.Cascade);

            base.OnModelCreating(modelBuilder);
        }
    }
}
