using System.Linq;
using MVC_project.Models;

namespace MVC_project.Data
{
    /// <summary>
    /// Repository for Trip entity operations.
    /// Implements the Repository Pattern to separate data access logic from business logic.
    /// This class encapsulates all database operations for trips, making the code more testable
    /// and maintainable by centralizing database queries in one place.
    /// </summary>
    public class TripRepository
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Constructor receives DbContext through dependency injection.
        /// The DbContext is configured in Program.cs with connection string and lifetime scope.
        /// </summary>
        public TripRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Adds a new trip to the database and immediately saves changes.
        /// SaveChanges() commits the transaction and generates the TripID from database auto-increment.
        /// </summary>
        public void Add(Trip trip)
        {
            _context.Trips.Add(trip);
            _context.SaveChanges();  // Persist to database immediately
        }

        /// <summary>
        /// Retrieves a single trip by its ID.
        /// Returns null if no trip is found with the given ID.
        /// FirstOrDefault() is used instead of Find() because it's more explicit in LINQ queries.
        /// </summary>
        public Trip? GetById(int id)
        {
            return _context.Trips.FirstOrDefault(t => t.TripID == id);
        }

        /// <summary>
        /// Retrieves all trips from the database, including inactive/deleted ones.
        /// ToList() executes the query immediately and loads all data into memory.
        /// Used primarily in admin dashboard where all trips need to be visible.
        /// </summary>
        public IEnumerable<Trip> GetAll()
        {
            return _context.Trips.ToList();
        }

        /// <summary>
        /// Retrieves only active trips (not soft-deleted).
        /// The Where clause filters trips where IsActive flag is true.
        /// Used in public-facing pages to hide deleted trips from customers.
        /// This is a "soft delete" approach - trips are marked inactive rather than removed from database.
        /// </summary>
        public IEnumerable<Trip> GetActiveTrips()
        {
            return _context.Trips.Where(t => t.IsActive).ToList();
        }

        /// <summary>
        /// Updates an existing trip's data.
        /// EF Core tracks which properties changed and generates an UPDATE SQL statement.
        /// SaveChanges() commits the modifications to the database.
        /// </summary>
        public void Update(Trip trip)
        {
            _context.Trips.Update(trip);
            _context.SaveChanges();
        }

        /// <summary>
        /// Permanently deletes a trip from the database (hard delete).
        /// This is rarely used - prefer SoftDelete to maintain historical data.
        /// Cascade delete will automatically remove related images, bookings, dates, etc.
        /// Returns silently if trip doesn't exist (idempotent operation).
        /// </summary>
        public void Delete(int id)
        {
            var trip = GetById(id);
            if (trip != null)
            {
                _context.Trips.Remove(trip);
                _context.SaveChanges();
            }
        }

        /// <summary>
        /// Soft deletes a trip by marking it as inactive.
        /// This preserves the trip data in the database for historical/reporting purposes
        /// while hiding it from public view. Bookings and related data remain intact.
        /// Preferred over hard delete to maintain data integrity and customer booking history.
        /// </summary>
        public void SoftDelete(int id)
        {
            var trip = GetById(id);
            if (trip != null)
            {
                trip.IsActive = false;
                Update(trip);  // Reuse Update method to save changes
            }
        }

        /// <summary>
        /// Toggles trip visibility without changing active status.
        /// IsVisible controls whether a trip appears in customer searches/listings.
        /// Admins use this to temporarily hide trips (sold out, seasonal) without deleting them.
        /// Returns the new visibility state so caller knows whether trip is now visible or hidden.
        /// </summary>
        public bool ToggleVisibility(int id)
        {
            var trip = GetById(id);
            if (trip != null)
            {
                trip.IsVisible = !trip.IsVisible;  // Flip the boolean
                Update(trip);
                return trip.IsVisible;  // Return new state
            }
            return false;  // Trip not found
        }

        /// <summary>
        /// Returns trips that have reminder configuration enabled (ReminderDaysBefore is set).
        /// Used by reminder services to find candidates without scanning all trips.
        /// </summary>
        public IEnumerable<Trip> GetReminderEnabledTrips()
        {
            return _context.Trips.Where(t => t.ReminderDaysBefore.HasValue).ToList();
        }
    }
}
