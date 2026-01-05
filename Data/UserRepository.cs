using MVC_project.Models;
using System;
using System.Linq;

namespace MVC_project.Data
{
    public class UserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        // Check if email already exists (for registration)
        public bool EmailExists(string email)
        {
            return _context.Users.Any(u => u.email == email);
        }

        // Add a new user
        public void Add(User user)
        {
            _context.Users.Add(user);
            _context.SaveChanges();
        }

        // Get user by email (for login) - case-insensitive
        public User GetByEmail(string email)
        {
            var normalizedEmail = email.Trim().ToLower();
            return _context.Users.FirstOrDefault(u => u.email.ToLower() == normalizedEmail);
        }

        // Get user by Id (for other operations)
        public User GetById(int id)
        {
            return _context.Users.FirstOrDefault(u => u.Id == id);
        }

        // Get all users
        public IEnumerable<User> GetAll()
        {
            return _context.Users
                .OrderBy(u => u.first_name)
                .ThenBy(u => u.last_name)
                .ToList();
        }

        // Search users by name or email
        public IEnumerable<User> Search(string? name, string? email)
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(email))
            {
                var emailLower = email.ToLower();
                query = query.Where(u => u.email.ToLower().Contains(emailLower));
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                var nameLower = name.ToLower();
                query = query.Where(u =>
                    (u.first_name != null && u.first_name.ToLower().Contains(nameLower)) ||
                    (u.last_name != null && u.last_name.ToLower().Contains(nameLower)));
            }

            return query
                .OrderBy(u => u.first_name)
                .ThenBy(u => u.last_name)
                .ToList();
        }

        // Update user (use Id)
        public void Update(User user)
        {
            _context.Users.Update(user);
            _context.SaveChanges();
        }

        // Delete user by Id
        public void Delete(int id)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user != null)
            {
                _context.Users.Remove(user);
                _context.SaveChanges();
            }
        }

        // Delete user along with related data to avoid FK conflicts (bookings, waitlist, ratings, user trips)
        public bool DeleteWithDependencies(int id)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return false;
            }

            // Remove dependent rows first to satisfy FK constraints
            var bookings = _context.Bookings.Where(b => b.UserId == id);
            var waitlists = _context.Waitlist.Where(w => w.UserId == id);
            var ratings = _context.TripRatings.Where(r => r.UserId == id);
            var userTrips = _context.UserTrips.Where(ut => ut.UserId == id);

            _context.Bookings.RemoveRange(bookings);
            _context.Waitlist.RemoveRange(waitlists);
            _context.TripRatings.RemoveRange(ratings);
            _context.UserTrips.RemoveRange(userTrips);
            _context.Users.Remove(user);

            _context.SaveChanges();
            return true;
        }
    }
}
