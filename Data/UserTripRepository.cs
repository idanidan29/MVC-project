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

        // Add a trip to user's cart
        public bool Add(string userEmail, int tripId)
        {
            // Check if already exists
            if (Exists(userEmail, tripId))
            {
                return false; // Already in cart
            }

            var userTrip = new UserTrip
            {
                UserEmail = userEmail,
                TripID = tripId
            };

            _context.UserTrips.Add(userTrip);
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
    }
}
