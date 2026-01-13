using MVC_project.Models;
using System;
using System.Linq;

namespace MVC_project.Data
{
    /// <summary>
    /// Repository for User entity operations.
    /// Handles user authentication, registration, search, and account management.
    /// All email comparisons are case-insensitive and trimmed to prevent duplicate accounts
    /// with slight variations (e.g., "User@example.com" vs "user@example.com").
    /// </summary>
    public class UserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Checks if an email address already exists in the database.
        /// Used during registration to prevent duplicate accounts.
        /// Any() is more efficient than FirstOrDefault() when we only need a boolean result
        /// because it stops searching as soon as it finds a match.
        /// </summary>
        public bool EmailExists(string email)
        {
            return _context.Users.Any(u => u.email == email);
        }

        /// <summary>
        /// Creates a new user account in the database.
        /// Password should already be hashed before calling this method (done in controller).
        /// SaveChanges() generates the auto-increment Id value from the database.
        /// </summary>
        public void Add(User user)
        {
            _context.Users.Add(user);
            _context.SaveChanges();
        }

        /// <summary>
        /// Retrieves a user by email address (case-insensitive).
        /// Used during login authentication to find the user account.
        /// Both the input email and stored email are converted to lowercase to ensure
        /// "User@example.com" matches "user@example.com".
        /// Trim() removes any accidental whitespace that users might enter.
        /// Returns null if no user found with that email.
        /// </summary>
        public User GetByEmail(string email)
        {
            var normalizedEmail = email.Trim().ToLower();  // Remove whitespace and convert to lowercase for case-insensitive comparison
            return _context.Users.FirstOrDefault(u => u.email.ToLower() == normalizedEmail);  // Find user with matching email (case-insensitive)
        }

        /// <summary>
        /// Retrieves a user by their unique ID.
        /// Used when we already know the user ID (e.g., from session storage).
        /// More efficient than email lookup since Id is the primary key (indexed).
        /// </summary>
        public User GetById(int id)
        {
            return _context.Users.FirstOrDefault(u => u.Id == id);
        }

        /// <summary>
        /// Retrieves all users sorted alphabetically by name.
        /// OrderBy sorts by first name first, then ThenBy sorts by last name
        /// within each first name group (e.g., "John Adams" comes before "John Smith").
        /// Used in admin dashboard to display user list.
        /// </summary>
        public IEnumerable<User> GetAll()
        {
            return _context.Users
                .OrderBy(u => u.first_name)
                .ThenBy(u => u.last_name)
                .ToList();
        }

        /// <summary>
        /// Searches users by name and/or email with case-insensitive partial matching.
        /// AsQueryable() allows building the query dynamically before executing it.
        /// 
        /// LINQ Query Building:
        /// 1. Start with all users as IQueryable (query not executed yet)
        /// 2. If email provided: filter to users whose email contains the search term
        /// 3. If name provided: filter to users whose first OR last name contains the search term
        /// 4. Sort by first name, then last name
        /// 5. ToList() executes the query and retrieves results
        /// 
        /// Contains() generates SQL LIKE '%term%' for partial matching.
        /// ToLower() ensures case-insensitive search.
        /// Null checks prevent errors when first_name or last_name are NULL.
        /// </summary>
        public IEnumerable<User> Search(string? name, string? email)
        {
            var query = _context.Users.AsQueryable();  // Start with all users as deferred query (not executed yet)

            // Filter by email if provided
            if (!string.IsNullOrWhiteSpace(email))  // Check if email parameter has actual content
            {
                var emailLower = email.ToLower();  // Convert search term to lowercase once
                query = query.Where(u => u.email.ToLower().Contains(emailLower));  // Add WHERE clause to query (SQL: WHERE email LIKE '%term%')
            }

            // Filter by name if provided (checks both first and last name)
            if (!string.IsNullOrWhiteSpace(name))  // Check if name parameter has actual content
            {
                var nameLower = name.ToLower();  // Convert search term to lowercase once
                query = query.Where(u =>  // Add WHERE clause with OR condition
                    (u.first_name != null && u.first_name.ToLower().Contains(nameLower)) ||  // Match first name if not null
                    (u.last_name != null && u.last_name.ToLower().Contains(nameLower)));    // OR match last name if not null
            }

            // Execute query with sorting
            return query
                .OrderBy(u => u.first_name)   // Primary sort by first name (SQL: ORDER BY first_name)
                .ThenBy(u => u.last_name)     // Secondary sort by last name within same first name
                .ToList();                     // Execute query and materialize results into memory
        }

        /// <summary>
        /// Updates an existing user's information.
        /// EF Core tracks changes and generates UPDATE SQL for modified properties only.
        /// Id cannot be changed - it's the primary key.
        /// </summary>
        public void Update(User user)
        {
            _context.Users.Update(user);
            _context.SaveChanges();
        }

        /// <summary>
        /// Deletes a user by ID (simple version).
        /// WARNING: This may fail if user has bookings, ratings, or cart items due to
        /// foreign key constraints. Use DeleteWithDependencies for safe deletion.
        /// </summary>
        public void Delete(int id)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user != null)
            {
                _context.Users.Remove(user);
                _context.SaveChanges();
            }
        }

        /// <summary>
        /// Safely deletes a user and all their related data.
        /// This method manually removes dependent records before deleting the user
        /// to prevent foreign key constraint violations.
        /// 
        /// Deletion Order (must delete child records before parent):
        /// 1. Bookings - user's confirmed trip reservations
        /// 2. Waitlist entries - user's waitlist registrations
        /// 3. Trip ratings - user's reviews of trips
        /// 4. UserTrips - items in user's shopping cart
        /// 5. User account - finally delete the user itself
        /// 
        /// Why manual deletion?
        /// Although we configured cascade delete in AppDbContext, explicitly removing
        /// dependent records gives us more control and makes the deletion process clearer.
        /// RemoveRange() is more efficient than removing items one by one in a loop.
        /// 
        /// Returns true if user was found and deleted, false if user doesn't exist.
        /// </summary>
        public bool DeleteWithDependencies(int id)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return false;  // User doesn't exist
            }

            // Find all records that reference this user
            var bookings = _context.Bookings.Where(b => b.UserId == id);
            var waitlists = _context.Waitlist.Where(w => w.UserId == id);
            var ratings = _context.TripRatings.Where(r => r.UserId == id);
            var userTrips = _context.UserTrips.Where(ut => ut.UserId == id);

            // Remove all dependent records first
            _context.Bookings.RemoveRange(bookings);
            _context.Waitlist.RemoveRange(waitlists);
            _context.TripRatings.RemoveRange(ratings);
            _context.UserTrips.RemoveRange(userTrips);
            
            // Finally remove the user
            _context.Users.Remove(user);

            // Commit all deletions in a single transaction
            _context.SaveChanges();
            return true;
        }
    }
}
