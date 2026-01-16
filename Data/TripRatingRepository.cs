using Microsoft.EntityFrameworkCore;
using MVC_project.Models;

namespace MVC_project.Data
{
    /// <summary>
    /// Repository for TripRating entity operations.
    /// Manages customer reviews and ratings for trips. Each user can rate a trip once,
    /// and the aggregate rating data is cached in the Trip entity for performance.
    /// </summary>
    public class TripRatingRepository
    {
        private readonly AppDbContext _context;

        public TripRatingRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Adds a new rating or updates an existing rating from the same user.
        /// Users can change their rating/review - we don't keep history, just current opinion.
        /// After saving, we recalculate aggregate rating statistics for the trip.
        /// </summary>
        public void AddOrUpdateRating(int tripId, int userId, byte rating, string? comment = null)
        {
            // Check if this user already rated this trip
            var existing = _context.TripRatings
                .FirstOrDefault(r => r.TripID == tripId && r.UserId == userId);  // Find existing rating

            if (existing != null)  // User already rated this trip
            {
                existing.Rating = rating;                  // Update rating value (1-5 stars)
                existing.Comment = comment;                 // Update review text
                existing.CreatedAt = DateTime.UtcNow;      // Update timestamp to show "Edited"
            }
            else  // New rating from this user
            {
                var newRating = new TripRating
                {
                    TripID = tripId,
                    UserId = userId,
                    Rating = rating,
                    Comment = comment,
                    CreatedAt = DateTime.UtcNow  // Use UTC to avoid timezone issues
                };
                _context.TripRatings.Add(newRating);  // Add new rating
            }

            _context.SaveChanges();           // Persist rating
            RecalculateAggregates(tripId);    // Update Trip.RatingSum, Trip.RatingCount
        }

        /// <summary>
        /// Retrieves a specific user's rating for a trip.
        /// Include(r => r.User) loads user info in same query (for displaying reviewer name).
        /// Returns null if user hasn't rated this trip yet.
        /// </summary>
        public TripRating? GetUserRating(int tripId, int userId)
        {
            return _context.TripRatings
                .Include(r => r.User)  // Eager load user data (avoids N+1 query if we access rating.User later)
                .FirstOrDefault(r => r.TripID == tripId && r.UserId == userId);  // Find user's rating
        }

        /// <summary>
        /// Retrieves all ratings for a trip with user information.
        /// Sorted by newest first so recent reviews appear at top.
        /// Used on trip details page to display all customer reviews.
        /// </summary>
        public List<TripRating> GetByTripId(int tripId)
        {
            return _context.TripRatings
                .Include(r => r.User)                  // Eager load user for each rating
                .Where(r => r.TripID == tripId)       // Filter to specified trip
                .OrderByDescending(r => r.CreatedAt)   // Newest reviews first
                .ToList();                             // Execute and materialize
        }

        /// <summary>
        /// Deletes a rating (admin moderation feature).
        /// After deletion, recalculates trip's aggregate rating to reflect removal.
        /// </summary>
        public void DeleteRating(int tripRatingId)
        {
            var rating = _context.TripRatings.Find(tripRatingId);  // Find by primary key (most efficient)
            if (rating != null)
            {
                var tripId = rating.TripID;               // Save trip ID before deletion
                _context.TripRatings.Remove(rating);      // Mark for deletion
                _context.SaveChanges();                    // Execute DELETE
                RecalculateAggregates(tripId);            // Update Trip.RatingSum and RatingCount
            }
        }

        /// <summary>
        /// Deletes all ratings for a trip (used when deleting a trip).
        /// Returns count of deleted ratings.
        /// </summary>
        public int DeleteByTripId(int tripId)
        {
            var ratings = _context.TripRatings.Where(r => r.TripID == tripId).ToList();
            if (!ratings.Any()) return 0;

            _context.TripRatings.RemoveRange(ratings);
            _context.SaveChanges();
            return ratings.Count;
        }

        /// <summary>
        /// Recalculates and updates aggregate rating statistics for a trip.
        /// 
        /// Why cache aggregates?
        /// Instead of calculating average rating with COUNT(*) and SUM(rating) every time
        /// we display a trip, we store the sum and count in the Trip table.
        /// This is a denormalization trade-off: slightly more complex updates for much faster reads.
        /// 
        /// Called after every add/update/delete to keep cached data in sync.
        /// </summary>
        private void RecalculateAggregates(int tripId)
        {
            var ratings = _context.TripRatings
                .Where(r => r.TripID == tripId)  // Get all ratings for this trip
                .ToList();                        // Load into memory for Sum/Max operations

            var trip = _context.Trips.Find(tripId);  // Get trip entity to update
            if (trip != null)
            {
                trip.RatingSum = ratings.Sum(r => r.Rating);                           // Total of all rating values (e.g., 5+4+5 = 14)
                trip.RatingCount = ratings.Count;                                       // Number of ratings (e.g., 3)
                trip.LastRatedAt = ratings.Any() ? ratings.Max(r => r.CreatedAt) : null;  // Most recent rating timestamp (null if no ratings)
                _context.SaveChanges();  // Update Trip record
            }
        }

        /// <summary>
        /// Calculates average rating for a trip from cached aggregates.
        /// Average = RatingSum / RatingCount (e.g., 14 / 3 = 4.67 stars).
        /// Returns 0 if trip has no ratings yet.
        /// 
        /// This is much faster than SELECT AVG(Rating) because we use pre-calculated values.
        /// </summary>
        public decimal GetAverageRating(int tripId)
        {
            var trip = _context.Trips.Find(tripId);  // Get trip with cached rating data
            if (trip == null || trip.RatingCount == 0)  // No trip or no ratings
                return 0;

            return trip.RatingSum / (decimal)trip.RatingCount;  // Calculate average (cast to decimal for precision)
        }
    }
}
