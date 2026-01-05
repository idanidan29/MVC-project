using Microsoft.EntityFrameworkCore;
using MVC_project.Models;

namespace MVC_project.Data
{
    public class TripRatingRepository
    {
        private readonly AppDbContext _context;

        public TripRatingRepository(AppDbContext context)
        {
            _context = context;
        }

        // Add or update user rating for a trip
        public void AddOrUpdateRating(int tripId, int userId, byte rating, string? comment = null)
        {
            var existing = _context.TripRatings
                .FirstOrDefault(r => r.TripID == tripId && r.UserId == userId);

            if (existing != null)
            {
                existing.Rating = rating;
                existing.Comment = comment;
                existing.CreatedAt = DateTime.UtcNow;
            }
            else
            {
                var newRating = new TripRating
                {
                    TripID = tripId,
                    UserId = userId,
                    Rating = rating,
                    Comment = comment,
                    CreatedAt = DateTime.UtcNow
                };
                _context.TripRatings.Add(newRating);
            }

            _context.SaveChanges();
            RecalculateAggregates(tripId);
        }

        // Get user's rating for a trip
        public TripRating? GetUserRating(int tripId, int userId)
        {
            return _context.TripRatings
                .Include(r => r.User)
                .FirstOrDefault(r => r.TripID == tripId && r.UserId == userId);
        }

        // Get all ratings for a trip
        public List<TripRating> GetByTripId(int tripId)
        {
            return _context.TripRatings
                .Include(r => r.User)
                .Where(r => r.TripID == tripId)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();
        }

        // Delete rating
        public void DeleteRating(int tripRatingId)
        {
            var rating = _context.TripRatings.Find(tripRatingId);
            if (rating != null)
            {
                var tripId = rating.TripID;
                _context.TripRatings.Remove(rating);
                _context.SaveChanges();
                RecalculateAggregates(tripId);
            }
        }

        // Recalculate and update trip aggregates
        private void RecalculateAggregates(int tripId)
        {
            var ratings = _context.TripRatings
                .Where(r => r.TripID == tripId)
                .ToList();

            var trip = _context.Trips.Find(tripId);
            if (trip != null)
            {
                trip.RatingSum = ratings.Sum(r => r.Rating);
                trip.RatingCount = ratings.Count;
                trip.LastRatedAt = ratings.Any() ? ratings.Max(r => r.CreatedAt) : null;
                _context.SaveChanges();
            }
        }

        // Get average rating for a trip
        public decimal GetAverageRating(int tripId)
        {
            var trip = _context.Trips.Find(tripId);
            if (trip == null || trip.RatingCount == 0)
                return 0;

            return trip.RatingSum / (decimal)trip.RatingCount;
        }
    }
}
