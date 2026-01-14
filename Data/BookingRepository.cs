using System;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using MVC_project.Models;

namespace MVC_project.Data
{
    /// <summary>
    /// Repository for Booking entity operations.
    /// Handles confirmed trip reservations after payment completion.
    /// Bookings represent the final confirmed state - UserTrips are the shopping cart,
    /// Bookings are created after successful payment.
    /// </summary>
    public class BookingRepository
    {
        private readonly AppDbContext _context;

        public BookingRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Creates a new booking after successful payment.
        /// Returns the booking with its generated BookingID from database.
        /// SaveChanges() persists to database and populates auto-increment ID.
        /// </summary>
        public Booking Add(Booking booking)
        {
            _context.Bookings.Add(booking);
            _context.SaveChanges();
            return booking;  // Return booking with populated BookingID
        }

        /// <summary>
        /// Updates an existing booking (e.g., status change, cancellation).
        /// Used to mark bookings as cancelled or update payment status.
        /// </summary>
        public void Update(Booking booking)
        {
            _context.Bookings.Update(booking);
            _context.SaveChanges();
        }

        /// <summary>
        /// Retrieves all bookings for a specific user, including trip details.
        /// 
        /// Include(b => b.Trip) performs eager loading - it loads the Trip data
        /// in the same database query to avoid N+1 query problem.
        /// Without Include, accessing booking.Trip would trigger a separate query for each booking.
        /// 
        /// OrderByDescending shows newest bookings first for better user experience.
        /// </summary>
        public IEnumerable<Booking> GetByUserId(int userId)
        {
            return _context.Bookings
                .Include(b => b.Trip)  // Eager load trip details
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.BookingDate)  // Most recent first
                .ToList();
        }

        /// <summary>
        /// Retrieves only active (non-cancelled) bookings for a user.
        /// Status != "Cancelled" filters out cancelled bookings.
        /// Used in user dashboard to show current/upcoming trips only.
        /// </summary>
        public IEnumerable<Booking> GetActiveByUserId(int userId)
        {
            return _context.Bookings
                .Include(b => b.Trip)
                .Where(b => b.UserId == userId && b.Status != "Cancelled")
                .OrderByDescending(b => b.BookingDate)
                .ToList();
        }

        /// <summary>
        /// Counts the number of distinct active trips a user has booked.
        /// 
        /// Why Distinct()?
        /// A user might book the same trip multiple times (e.g., bringing friends).
        /// We want to count unique trips, not total bookings.
        /// 
        /// Status ?? string.Empty handles null Status values safely
        /// by treating null as empty string before comparing to "Cancelled".
        /// </summary>
        public int CountActiveByUserId(int userId)
        {
            return _context.Bookings
                .Where(b => b.UserId == userId && (b.Status ?? string.Empty) != "Cancelled")  // Filter: user's bookings that aren't cancelled (null-coalescing handles null Status)
                .Select(b => b.TripID)  // Project to just trip IDs (reduces data transferred from database)
                .Distinct()              // Remove duplicate trip IDs (user may book same trip multiple times)
                .Count();                // Count the unique trip IDs (SQL: SELECT COUNT(DISTINCT TripID))
        }

        /// <summary>
        /// Counts upcoming confirmed trips for a user (trips that haven't ended yet).
        /// 
        /// Why check EndDate >= todayUtc.Date?
        /// We want to count trips that are still ongoing or in the future.
        /// Trip is considered "upcoming" until its end date passes.
        /// 
        /// b.Trip != null ensures we don't crash if Trip wasn't loaded.
        /// Status must be "Confirmed" to exclude pending/cancelled bookings.
        /// Used to display "You have X upcoming trips" on dashboard.
        /// </summary>
        public int CountUpcoming(int userId, DateTime todayUtc)
        {
            return _context.Bookings
                .Include(b => b.Trip)  // Need trip data to check end date
                .Count(b => b.UserId == userId 
                    && b.Trip != null 
                    && b.Trip.EndDate >= todayUtc.Date
                    && (b.Status ?? string.Empty) == "Confirmed");
        }

        /// <summary>
        /// Retrieves confirmed bookings for a trip that are still upcoming, including user details for emailing reminders.
        /// </summary>
        public IEnumerable<Booking> GetConfirmedByTripId(int tripId, DateTime todayUtc)
        {
            var completedStatuses = new[] { "Confirmed", "Booked" };

            return _context.Bookings
                .Include(b => b.Trip)
                .Include(b => b.User)
                .Where(b => b.TripID == tripId
                    && b.Trip != null
                    && b.Trip.StartDate >= todayUtc.Date
                    && completedStatuses.Contains((b.Status ?? string.Empty)))
                .ToList();
        }

        /// <summary>
        /// Gets the total number of confirmed bookings for multiple trips.
        /// Returns a dictionary where Key = TripID, Value = booking count.
        /// 
        /// LINQ Query Breakdown:
        /// 1. Filter bookings to only those for specified trip IDs
        /// 2. Filter to completed statuses ("Confirmed" or "Booked")
        /// 3. GroupBy groups bookings by TripID
        /// 4. Select creates anonymous objects with TripId and Count
        /// 5. ToDictionary converts to Dictionary for fast lookup by TripID
        /// 
        /// Why this design?
        /// When displaying multiple trips on a page, we need booking counts for all of them.
        /// This method gets all counts in ONE database query instead of querying each trip separately.
        /// Much more efficient than calling a count method for each trip individually.
        /// </summary>
        public Dictionary<int, int> GetCompletedBookingCounts(IEnumerable<int> tripIds)
        {
            var ids = tripIds?.Distinct().ToList();  // Remove duplicate trip IDs and materialize to list for Contains() efficiency
            if (ids == null || ids.Count == 0)
                return new Dictionary<int, int>();  // Return empty dictionary if no IDs provided

            var completedStatuses = new[] { "Confirmed", "Booked" };  // Define which statuses count as completed

            return _context.Bookings
                .Where(b => ids.Contains(b.TripID)  // Filter to only trips we're interested in (SQL: WHERE TripID IN (...))
                    && completedStatuses.Contains((b.Status ?? string.Empty)))  // AND status is completed (handles null Status)
                .GroupBy(b => b.TripID)  // Group bookings by trip (SQL: GROUP BY TripID)
                .Select(g => new { TripId = g.Key, Count = g.Count() })  // For each group, get trip ID and count (SQL: SELECT TripID, COUNT(*))
                .ToDictionary(x => x.TripId, x => x.Count);  // Convert to dictionary for O(1) lookup by TripID
        }

        /// <summary>
        /// Gets the number of unique users who booked each trip.
        /// Similar to GetCompletedBookingCounts but counts distinct users instead of total bookings.
        /// 
        /// Why count distinct users?
        /// A single user might book the same trip multiple times (e.g., group booking).
        /// We want to know "how many different people booked this trip", not total booking count.
        /// 
        /// g.Select(b => b.UserId).Distinct().Count() for each group:
        /// - g.Select gets all UserIds in the group
        /// - Distinct removes duplicate UserIds
        /// - Count gives us the number of unique users
        /// 
        /// Used for trip popularity metrics in admin dashboard.
        /// </summary>
        public Dictionary<int, int> GetCompletedDistinctUserCounts(IEnumerable<int> tripIds)
        {
            var ids = tripIds?.Distinct().ToList();  // Remove duplicate trip IDs and materialize for efficient Contains()
            if (ids == null || ids.Count == 0)
                return new Dictionary<int, int>();

            var completedStatuses = new[] { "Confirmed", "Booked" };  // Statuses that count as completed bookings

            return _context.Bookings
                .Where(b => ids.Contains(b.TripID)  // Filter to specified trips
                    && completedStatuses.Contains((b.Status ?? string.Empty)))  // AND completed status
                .GroupBy(b => b.TripID)  // Group by trip
                .Select(g => new { TripId = g.Key, Count = g.Select(b => b.UserId).Distinct().Count() })  // Count distinct user IDs per group (handles multiple bookings by same user)
                .ToDictionary(x => x.TripId, x => x.Count);  // Convert to dictionary for fast lookup
        }

        /// <summary>
        /// Retrieves a specific booking with full details for a user.
        /// Includes both Trip and User navigation properties via eager loading.
        /// 
        /// Why check both BookingID and UserId?
        /// Security: ensures users can only access their own bookings.
        /// A user should never be able to view/modify another user's booking
        /// even if they guess the BookingID.
        /// 
        /// Returns null if booking doesn't exist or doesn't belong to the user.
        /// </summary>
        public Booking? GetByIdForUser(int bookingId, int userId)
        {
            return _context.Bookings
                .Include(b => b.Trip)   // Load trip details
                .Include(b => b.User)   // Load user details
                .FirstOrDefault(b => b.BookingID == bookingId && b.UserId == userId);
        }

        /// <summary>
        /// Determines if a user can cancel their booking based on cancellation policy.
        /// 
        /// Business Rules:
        /// 1. Booking must exist and belong to the user (security check)
        /// 2. Booking must have an associated trip
        /// 3. Booking cannot already be cancelled (no double cancellation)
        /// 4. Current date must be on or before the trip's cancellation deadline
        /// 
        /// EffectiveCancellationEndDate is a computed property on Trip that considers:
        /// - Custom CancellationDeadline if set by admin, OR
        /// - Default deadline (e.g., 7 days before StartDate)
        /// 
        /// Why Date comparison?
        /// We use .Date to ignore time component and only compare dates.
        /// This means users can cancel any time on the deadline day.
        /// 
        /// Returns false if cancellation is not allowed for any reason.
        /// </summary>
        public bool CanCancelBooking(int bookingId, int userId, DateTime utcNow)
        {
            var booking = _context.Bookings
                .Include(b => b.Trip)  // Need trip to check cancellation deadline
                .FirstOrDefault(b => b.BookingID == bookingId && b.UserId == userId);

            if (booking == null || booking.Trip == null)
                return false;  // Booking or trip doesn't exist

            var status = booking.Status ?? string.Empty;
            if (status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
                return false;  // Already cancelled

            var today = utcNow.Date;
            return today <= booking.Trip.EffectiveCancellationEndDate.Date;
        }

        /// <summary>
        /// Deletes all bookings associated with a specific trip.
        /// Used when admin permanently deletes a trip from the system.
        /// 
        /// Why manual deletion?
        /// Although cascade delete is configured in AppDbContext, this method
        /// gives explicit control and returns the number of bookings deleted
        /// for logging/confirmation purposes.
        /// 
        /// Returns count of deleted bookings (0 if trip had no bookings).
        /// </summary>
        public int DeleteByTripId(int tripId)
        {
            var bookings = _context.Bookings.Where(b => b.TripID == tripId).ToList();
            if (bookings.Count == 0) return 0;
            
            _context.Bookings.RemoveRange(bookings);
            _context.SaveChanges();
            return bookings.Count;
        }
    }
}
