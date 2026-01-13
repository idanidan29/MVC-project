using MVC_project.Models;
using Microsoft.EntityFrameworkCore;

namespace MVC_project.Data
{
    /// <summary>
    /// Repository for UserTrip entity operations.
    /// Manages the shopping cart functionality. UserTrips represent items in a user's cart BEFORE payment.
    /// After successful payment, items are converted to Bookings and removed from UserTrips.
    /// Cart items expire after 24 hours to free up reserved room capacity for other customers.
    /// </summary>
    public class UserTripRepository
    {
        private readonly AppDbContext _context;

        public UserTripRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Adds a trip to user's cart with default quantity of 1.
        /// Convenience method that delegates to the full Add overload.
        /// </summary>
        public bool Add(int userId, int tripId)
        {
            return Add(userId, tripId, 1);  // Default quantity = 1 person
        }

        /// <summary>
        /// Adds a trip to user's cart with specified quantity and optional date selection.
        /// 
        /// Smart cart behavior:
        /// - If user already has this exact trip+date combo, we increment the quantity
        /// - If user has same trip but different date, we add as separate cart item
        /// - Each add/update resets the 24-hour expiration timer
        /// 
        /// Why selectedDateIndex?
        /// Trips can have multiple departure dates. Index -1 means "use trip's main date",
        /// otherwise it's the index into trip's TripDates collection.
        /// </summary>
        public bool Add(int userId, int tripId, int quantity, int selectedDateIndex = -1)
        {
            // Check if this exact combination (trip + date) already exists in cart
            var existing = _context.UserTrips
                .FirstOrDefault(ut => ut.UserId == userId && ut.TripID == tripId && ut.SelectedDateIndex == selectedDateIndex);

            if (existing != null)  // User already has this trip+date combo in cart
            {
                existing.Quantity += quantity;                    // Add to existing quantity (e.g., user adds 2 more rooms)
                existing.ExpiresAt = DateTime.Now.AddHours(24);  // Reset expiration to 24 hours from now
                _context.SaveChanges();                           // Persist changes
                return true;
            }

            // Add as new cart entry (same trip but different date, or first time adding this trip)
            var userTrip = new UserTrip
            {
                UserId = userId,
                TripID = tripId,
                Quantity = quantity,
                SelectedDateIndex = selectedDateIndex,
                ExpiresAt = DateTime.Now.AddHours(24)  // Cart item expires in 24 hours if not purchased
            };

            _context.UserTrips.Add(userTrip);  // Add to change tracker
            _context.SaveChanges();             // Persist to database
            return true;
        }

        /// <summary>
        /// Updates the quantity for an existing cart item.
        /// Used when user changes quantity directly (e.g., increases from 2 to 3 rooms).
        /// Returns false if item not found in cart.
        /// </summary>
        public bool UpdateQuantity(int userId, int tripId, int quantity)
        {
            var userTrip = _context.UserTrips
                .FirstOrDefault(ut => ut.UserId == userId && ut.TripID == tripId);  // Find cart item

            if (userTrip == null) return false;  // Item not in cart

            userTrip.Quantity = quantity;  // Update quantity
            _context.SaveChanges();         // Persist change
            return true;
        }

        /// <summary>
        /// Checks if user already has a specific trip+date combination in their cart.
        /// Used to prevent duplicate cart entries or show "Already in cart" message.
        /// selectedDateIndex = -1 checks for main trip date.
        /// </summary>
        public bool Exists(int userId, int tripId, int selectedDateIndex = -1)
        {
            return _context.UserTrips
                .Any(ut => ut.UserId == userId && ut.TripID == tripId && ut.SelectedDateIndex == selectedDateIndex);  // Check existence
        }

        /// <summary>
        /// Retrieves all items in a user's shopping cart with trip details.
        /// Include(ut => ut.Trip) loads trip info (name, price, images) for cart display.
        /// Without Include, accessing userTrip.Trip would trigger separate query for each item (N+1 problem).
        /// </summary>
        public IEnumerable<UserTrip> GetByUserId(int userId)
        {
            return _context.UserTrips
                .Include(ut => ut.Trip)          // Eager load trip details for display
                .Where(ut => ut.UserId == userId)  // Filter to user's items
                .ToList();                         // Execute and materialize
        }

        /// <summary>
        /// Gets a specific cart item by user and trip ID.
        /// Returns first matching entry if multiple exist (shouldn't happen with proper constraints).
        /// Used to check quantity or details of a specific cart item.
        /// </summary>
        public UserTrip? GetByUserIdAndTripId(int userId, int tripId)
        {
            return _context.UserTrips
                .Include(ut => ut.Trip)  // Load trip data
                .FirstOrDefault(ut => ut.UserId == userId && ut.TripID == tripId);  // Find first match
        }

        /// <summary>
        /// Removes a specific cart entry by its unique ID.
        /// Returns the removed UserTrip WITH trip info so caller can restore AvailableRooms.
        /// 
        /// Why return the deleted item?
        /// When removing from cart, we need to add quantity back to Trip.AvailableRooms.
        /// Returning the full object with .Trip included lets caller access both Quantity and Trip.
        /// </summary>
        public UserTrip? RemoveByUserTripId(int userTripId)
        {
            var userTrip = _context.UserTrips
                .Include(ut => ut.Trip)                      // Load trip data for caller
                .FirstOrDefault(ut => ut.UserTripID == userTripId);  // Find by cart item ID
            
            if (userTrip == null)  // Item doesn't exist
            {
                return null;
            }

            _context.UserTrips.Remove(userTrip);  // Mark for deletion
            _context.SaveChanges();                // Execute DELETE
            return userTrip;  // Return deleted item with trip data
        }

        /// <summary>
        /// Removes the first cart entry matching user and trip ID.
        /// Simpler removal when you don't need the deleted item's data.
        /// Returns true if item was found and deleted, false if not found.
        /// </summary>
        public bool Remove(int userId, int tripId)
        {
            var userTrip = _context.UserTrips
                .FirstOrDefault(ut => ut.UserId == userId && ut.TripID == tripId);  // Find item

            if (userTrip == null)  // Not in cart
            {
                return false;
            }

            _context.UserTrips.Remove(userTrip);  // Mark for deletion
            _context.SaveChanges();                // Execute DELETE
            return true;
        }

        /// <summary>
        /// Gets the count of items in user's cart.
        /// Used for cart badge display ("Cart (3)" in navigation).
        /// Counts rows, not total quantity - 2 trips with 3 rooms each = count of 2.
        /// </summary>
        public int GetCount(int userId)
        {
            return _context.UserTrips.Count(ut => ut.UserId == userId);  // SQL: SELECT COUNT(*) WHERE UserId = @userId
        }

        /// <summary>
        /// Empties user's entire cart.
        /// Called after successful payment (cart items converted to bookings) or manual "Clear Cart".
        /// Returns count of items removed for confirmation message.
        /// </summary>
        public int RemoveAll(int userId)
        {
            var items = _context.UserTrips.Where(ut => ut.UserId == userId).ToList();  // Get all user's cart items
            if (!items.Any()) return 0;  // Cart already empty
            
            _context.UserTrips.RemoveRange(items);  // Mark all for deletion
            _context.SaveChanges();                  // Execute DELETE for all items
            return items.Count;  // Return how many items were removed
        }
    }
}
