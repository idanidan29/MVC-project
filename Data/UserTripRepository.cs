using MVC_project.Models;
using Microsoft.EntityFrameworkCore;

namespace MVC_project.Data
{
    public class UserTripRepository
    {
        private readonly AppDbContext _context;

        public UserTripRepository(AppDbContext context)
        {
            _context = context;
        }

        // Add a trip to user's cart (default quantity = 1)
        public bool Add(int userId, int tripId)
        {
            return Add(userId, tripId, 1);
        }

        // Add a trip to user's cart with quantity and selected date
        public bool Add(int userId, int tripId, int quantity, int selectedDateIndex = -1)
        {
            // Check if this exact combination (trip + date) already exists
            var existing = _context.UserTrips
                .FirstOrDefault(ut => ut.UserId == userId && ut.TripID == tripId && ut.SelectedDateIndex == selectedDateIndex);

            if (existing != null)
            {
                // Increment quantity for the same trip with same date
                existing.Quantity += quantity;
                _context.SaveChanges();
                return true;
            }

            // Add as new entry (same trip but different date)
            var userTrip = new UserTrip
            {
                UserId = userId,
                TripID = tripId,
                Quantity = quantity,
                SelectedDateIndex = selectedDateIndex
            };

            _context.UserTrips.Add(userTrip);
            _context.SaveChanges();
            return true;
        }

        public bool UpdateQuantity(int userId, int tripId, int quantity)
        {
            var userTrip = _context.UserTrips
                .FirstOrDefault(ut => ut.UserId == userId && ut.TripID == tripId);

            if (userTrip == null) return false;

            userTrip.Quantity = quantity;
            _context.SaveChanges();
            return true;
        }

        // Check if user already has this trip with this specific date in cart
        public bool Exists(int userId, int tripId, int selectedDateIndex = -1)
        {
            return _context.UserTrips
                .Any(ut => ut.UserId == userId && ut.TripID == tripId && ut.SelectedDateIndex == selectedDateIndex);
        }

        // Get all trips for a user
        public IEnumerable<UserTrip> GetByUserId(int userId)
        {
            return _context.UserTrips
                .Include(ut => ut.Trip)
                .Where(ut => ut.UserId == userId)
                .ToList();
        }

        // Get specific cart item by UserId and TripID (returns first match)
        public UserTrip? GetByUserIdAndTripId(int userId, int tripId)
        {
            return _context.UserTrips
                .Include(ut => ut.Trip)
                .FirstOrDefault(ut => ut.UserId == userId && ut.TripID == tripId);
        }

        // Remove a trip from user's cart by UserTripID (specific entry)
        public UserTrip? RemoveByUserTripId(int userTripId)
        {
            var userTrip = _context.UserTrips
                .Include(ut => ut.Trip)
                .FirstOrDefault(ut => ut.UserTripID == userTripId);
            
            if (userTrip == null)
            {
                return null;
            }

            _context.UserTrips.Remove(userTrip);
            _context.SaveChanges();
            return userTrip;
        }

        // Remove a trip from user's cart (all entries for this trip)
        public bool Remove(int userId, int tripId)
        {
            var userTrip = _context.UserTrips
                .FirstOrDefault(ut => ut.UserId == userId && ut.TripID == tripId);

            if (userTrip == null)
            {
                return false;
            }

            _context.UserTrips.Remove(userTrip);
            _context.SaveChanges();
            return true;
        }

        // Get count of trips in user's cart
        public int GetCount(int userId)
        {
            return _context.UserTrips.Count(ut => ut.UserId == userId);
        }

        // Remove all trips from user's cart
        public int RemoveAll(int userId)
        {
            var items = _context.UserTrips.Where(ut => ut.UserId == userId).ToList();
            if (!items.Any()) return 0;
            _context.UserTrips.RemoveRange(items);
            _context.SaveChanges();
            return items.Count;
        }
    }
}
