using Microsoft.EntityFrameworkCore;
using MVC_project.Models;

namespace MVC_project.Data
{
    /// <summary>
    /// Database context for the travel booking application.
    /// This class represents the session with the database and allows us to query and save data.
    /// Entity Framework Core uses this context to map C# objects to database tables.
    /// </summary>
    public class AppDbContext : DbContext
    {
        /// <summary>
        /// Constructor that receives configuration options from dependency injection.
        /// The options contain connection string and provider settings configured in Program.cs
        /// </summary>
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // DbSet properties represent tables in the database
        // Each DbSet<T> allows LINQ queries and CRUD operations on that table
        public DbSet<User> Users { get; set; }
        public DbSet<Trip> Trips { get; set; }
        public DbSet<TripImage> TripImages { get; set; }
        public DbSet<UserTrip> UserTrips { get; set; }  // Cart items (pending bookings)
        public DbSet<Booking> Bookings { get; set; }     // Confirmed bookings
        public DbSet<TripDate> TripDates { get; set; }   // Alternative date options for trips
        public DbSet<Waitlist> Waitlist { get; set; }    // Users waiting for fully booked trips
        public DbSet<TripRating> TripRatings { get; set; }
        public DbSet<UserFeedback> UserFeedback { get; set; }

        /// <summary>
        /// Configures the database schema and relationships using Fluent API.
        /// This method is called by EF Core when the context is being initialized.
        /// We use Fluent API here instead of data annotations because it provides more control
        /// and keeps the model classes clean.
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ===== USER ENTITY CONFIGURATION =====
            // Map User class to the dbo.Users table in the database
            modelBuilder.Entity<User>()
                .ToTable("Users", "dbo");

            // Define Id as the primary key - EF Core will auto-increment this value
            modelBuilder.Entity<User>()
                .HasKey(u => u.Id);

            // Enforce email uniqueness at database level to prevent duplicate accounts
            // This creates a unique index which is faster than checking duplicates in code
            modelBuilder.Entity<User>()
                .HasIndex(u => u.email)
                .IsUnique()
                .HasDatabaseName("UQ_Users_Email");

            // ===== TRIP ENTITY CONFIGURATION =====
            modelBuilder.Entity<Trip>()
                .ToTable("Trips", "dbo");

            modelBuilder.Entity<Trip>()
                .HasKey(t => t.TripID);

            // ===== TRIP IMAGE ENTITY CONFIGURATION =====
            // Trip images are stored as binary data (VARBINARY) in the database
            modelBuilder.Entity<TripImage>()
                .ToTable("TripImages", "dbo");

            modelBuilder.Entity<TripImage>()
                .HasKey(ti => ti.ImageID);

            // One Trip can have many TripImages (One-to-Many relationship)
            // When a trip is deleted, all its images are automatically deleted (Cascade)
            // This prevents orphaned image records in the database
            modelBuilder.Entity<TripImage>()
                .HasOne(ti => ti.Trip)
                .WithMany()
                .HasForeignKey(ti => ti.TripID)
                .OnDelete(DeleteBehavior.Cascade);

            // ===== USERTRIP ENTITY CONFIGURATION =====
            // UserTrip represents items in a user's shopping cart (before payment)
            modelBuilder.Entity<UserTrip>()
                .ToTable("UserTrips", "dbo");

            modelBuilder.Entity<UserTrip>()
                .HasKey(ut => ut.UserTripID);

            // Prevent a user from adding the same trip to their cart multiple times
            // The unique constraint on (UserId, TripID) enforces this at database level
            modelBuilder.Entity<UserTrip>()
                .HasIndex(ut => new { ut.UserId, ut.TripID })
                .IsUnique()
                .HasDatabaseName("UQ_UserTrips_User_Trip");

            // When a user is deleted, their cart items are automatically removed
            // When a trip is deleted, all cart items for that trip are removed
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

            // ===== BOOKING ENTITY CONFIGURATION =====
            // Booking represents a confirmed reservation (after payment)
            modelBuilder.Entity<Booking>()
                .ToTable("Bookings", "dbo");

            modelBuilder.Entity<Booking>()
                .HasKey(b => b.BookingID);

            // Each booking belongs to one user and one trip
            // Cascade delete ensures data integrity when users or trips are removed
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.User)
                .WithMany()
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Trip)
                .WithMany()
                .HasForeignKey(b => b.TripID)
                .OnDelete(DeleteBehavior.Cascade);

            // ===== TRIPDATE ENTITY CONFIGURATION =====
            // TripDate stores alternative date options for the same trip
            // This allows trips to have multiple departure dates with different availability
            modelBuilder.Entity<TripDate>()
                .ToTable("TripDates", "dbo");

            modelBuilder.Entity<TripDate>()
                .HasKey(td => td.TripDateID);

            // When a trip is deleted, all its alternative dates are removed
            modelBuilder.Entity<TripDate>()
                .HasOne(td => td.Trip)
                .WithMany()
                .HasForeignKey(td => td.TripID)
                .OnDelete(DeleteBehavior.Cascade);

            // ===== WAITLIST ENTITY CONFIGURATION =====
            // Waitlist tracks users interested in fully booked trips
            // When space becomes available, these users are notified in order
            modelBuilder.Entity<Waitlist>()
                .ToTable("Waitlist", "dbo");

            modelBuilder.Entity<Waitlist>()
                .HasKey(w => w.WaitlistID);

            // Cascade delete: If user or trip is deleted, their waitlist entries are removed
            // This prevents invalid waitlist entries pointing to non-existent records
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

            // Call base implementation to apply any additional EF Core conventions
            base.OnModelCreating(modelBuilder);
        }
    }
}
