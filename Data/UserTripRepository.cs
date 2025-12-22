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
        public bool Add(string userEmail, int tripId)
        {
            return Add(userEmail, tripId, 1);
        }

        // Add a trip to user's cart with quantity
        public bool Add(string userEmail, int tripId, int quantity)
        {
            // Check if already exists
            var existing = _context.UserTrips
                .FirstOrDefault(ut => ut.UserEmail == userEmail && ut.TripID == tripId);

            if (existing != null)
            {
                // Increment quantity instead of duplicating
                existing.Quantity += quantity;
                _context.SaveChanges();
                return true;
            }

            var userTrip = new UserTrip
            {
                UserEmail = userEmail,
                TripID = tripId,
                Quantity = quantity
            };

            _context.UserTrips.Add(userTrip);
            _context.SaveChanges();
            return true;
        }

        public bool UpdateQuantity(string userEmail, int tripId, int quantity)
        {
            var userTrip = _context.UserTrips
                .FirstOrDefault(ut => ut.UserEmail == userEmail && ut.TripID == tripId);

            if (userTrip == null) return false;

            userTrip.Quantity = quantity;
            _context.SaveChanges();
            return true;
        }

        // Check if user already has this trip in cart
        public bool Exists(string userEmail, int tripId)
        {
            return _context.UserTrips
                .Any(ut => ut.UserEmail == userEmail && ut.TripID == tripId);
        }

        // Get all trips for a user
        public IEnumerable<UserTrip> GetByUserEmail(string userEmail)
        {
            return _context.UserTrips
                .Include(ut => ut.Trip)
                .Where(ut => ut.UserEmail == userEmail)
                .ToList();
        }

        // Remove a trip from user's cart
        public bool Remove(string userEmail, int tripId)
        {
            var userTrip = _context.UserTrips
                .FirstOrDefault(ut => ut.UserEmail == userEmail && ut.TripID == tripId);

            if (userTrip == null)
            {
                return false;
            }

            _context.UserTrips.Remove(userTrip);
            _context.SaveChanges();
            return true;
        }

        // Get count of trips in user's cart
        public int GetCount(string userEmail)
        {
            return _context.UserTrips.Count(ut => ut.UserEmail == userEmail);
        }

        // Remove all trips from user's cart
        public int RemoveAll(string userEmail)
        {
            var items = _context.UserTrips.Where(ut => ut.UserEmail == userEmail).ToList();
            if (!items.Any()) return 0;
            _context.UserTrips.RemoveRange(items);
            _context.SaveChanges();
            return items.Count;
        }
    }
}
