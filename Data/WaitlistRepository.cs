using MVC_project.Models;
using Microsoft.EntityFrameworkCore;

namespace MVC_project.Data
{
    /// <summary>
    /// Repository for Waitlist entity operations.
    /// Manages the waitlist system for fully booked trips. When a trip is full, customers can join
    /// a waitlist and will be notified in FIFO (First In, First Out) order when space becomes available.
    /// 
    /// Waitlist States:
    /// - "Waiting" = User is in queue waiting for a spot
    /// - "Notified" = User has been notified of availability and has 24 hours to book
    /// - "Booked" = User successfully completed booking after notification
    /// - "Expired" = User's 24-hour booking window expired without action
    /// </summary>
    public class WaitlistRepository
    {
        private readonly AppDbContext _context;

        public WaitlistRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Checks if a user is currently on the waitlist for a specific trip.
        /// Only checks "Waiting" status - ignores users who were already notified/booked/expired.
        /// Used to prevent duplicate waitlist entries and show "You're on the waitlist" message.
        /// </summary>
        public bool IsUserOnWaitlist(int userId, int tripId)
        {
            return _context.Waitlist
                .Any(w => w.UserId == userId && w.TripId == tripId && w.Status == "Waiting");  // Check if actively waiting
        }

        /// <summary>
        /// Adds a user to the waitlist for a fully booked trip.
        /// 
        /// Process:
        /// 1. Check if user is already waiting (prevent duplicates)
        /// 2. Create waitlist entry with "Waiting" status
        /// 3. ExpiresAt is null while waiting (only set when notified)
        /// 4. CreatedAt determines position in queue (FIFO order)
        /// 
        /// Returns false if user already on waitlist, true if successfully added.
        /// </summary>
        public bool AddToWaitlist(int userId, int tripId)
        {
            // Check if already on waitlist to prevent duplicates
            if (IsUserOnWaitlist(userId, tripId))
            {
                return false;  // User already waiting
            }

            var waitlistEntry = new Waitlist
            {
                UserId = userId,
                TripId = tripId,
                Status = "Waiting",               // Initial state
                CreatedAt = DateTime.Now,         // Determines queue position
                ExpiresAt = null,                 // Not expiring yet (only when notified)
                EmailSentAt = null                // No email sent yet
            };

            _context.Waitlist.Add(waitlistEntry);  // Add to change tracker
            _context.SaveChanges();                 // Persist to database
            return true;
        }

        /// <summary>
        /// Gets the number of users currently waiting for a trip.
        /// Only counts "Waiting" status - excludes notified/booked/expired entries.
        /// Used to display "X people waiting" or calculate estimated wait time.
        /// </summary>
        public int GetWaitlistCountForTrip(int tripId)
        {
            return _context.Waitlist.Count(w => w.TripId == tripId && w.Status == "Waiting");  // Count active waiters
        }

        /// <summary>
        /// Gets all users who were notified but haven't been sent an email yet.
        /// 
        /// Why this design?
        /// Status changes to "Notified" immediately when spot becomes available,
        /// but email sending happens asynchronously (might fail/retry).
        /// EmailSentAt = null indicates email still needs to be sent.
        /// 
        /// Background service periodically calls this to send pending emails.
        /// Include() loads User and Trip data needed for email content.
        /// </summary>
        public List<Waitlist> GetPendingNotifications()
        {
            return _context.Waitlist
                .Include(w => w.User)                                         // Need user email address
                .Include(w => w.Trip)                                         // Need trip details for email
                .Where(w => w.Status == "Notified" && w.EmailSentAt == null)  // Notified but email not sent yet
                .ToList();
        }

        /// <summary>
        /// Marks email as sent and sets 24-hour expiration timer.
        /// 
        /// After email is successfully sent:
        /// 1. EmailSentAt = now (records when email was sent)
        /// 2. ExpiresAt = now + 24 hours (user has 24 hours to book)
        /// 3. Status remains "Notified"
        /// 
        /// Background service will check ExpiresAt to expire old notifications.
        /// </summary>
        public void MarkEmailSent(int waitlistId)
        {
            var entry = _context.Waitlist.Find(waitlistId);  // Find by primary key
            if (entry != null)
            {
                entry.EmailSentAt = DateTime.Now;              // Record email send time
                entry.ExpiresAt = DateTime.Now.AddHours(24);   // Set 24-hour booking window
                entry.Status = "Notified";                     // Ensure status is set (should already be Notified)
                _context.SaveChanges();
            }
        }

        /// <summary>
        /// Updates the status of a waitlist entry.
        /// Used to transition between states:
        /// - "Waiting" → "Notified" (spot available)
        /// - "Notified" → "Booked" (user completed booking)
        /// - "Notified" → "Expired" (24 hours passed without booking)
        /// </summary>
        public void UpdateStatus(int waitlistId, string status)
        {
            var entry = _context.Waitlist.Find(waitlistId);  // Find by primary key (efficient)
            if (entry != null)
            {
                entry.Status = status;  // Update status
                _context.SaveChanges();  // Persist change
            }
        }

        /// <summary>
        /// Gets the next user in line for a trip (FIFO queue).
        /// 
        /// FIFO Implementation:
        /// - OrderBy(w => w.CreatedAt) sorts by join time (earliest first)
        /// - FirstOrDefault() gets the person who's been waiting longest
        /// 
        /// Only considers "Waiting" status - skips users already notified.
        /// Returns null if no one is waiting.
        /// Include() loads user and trip data for notification.
        /// </summary>
        public Waitlist? GetNextWaitingUser(int tripId)
        {
            return _context.Waitlist
                .Include(w => w.User)                  // Load user data for notification
                .Include(w => w.Trip)                  // Load trip data for notification
                .Where(w => w.TripId == tripId && w.Status == "Waiting")  // Only actively waiting users
                .OrderBy(w => w.CreatedAt)             // Sort by join time (FIFO)
                .FirstOrDefault();                     // Get the first (longest waiting)
        }

        /// <summary>
        /// Notifies the next user in queue that a spot is available.
        /// 
        /// Process:
        /// 1. Get next waiting user (FIFO)
        /// 2. Change status to "Notified"
        /// 3. Actual email is sent by background service (checks GetPendingNotifications)
        /// 
        /// Called when:
        /// - A booking is cancelled
        /// - Admin increases trip capacity
        /// - Previous notified user's 24-hour window expired
        /// </summary>
        public void NotifyNextUser(int tripId)
        {
            var nextUser = GetNextWaitingUser(tripId);  // Get next in line
            if (nextUser != null)
            {
                nextUser.Status = "Notified";  // Mark as notified (email will be sent asynchronously)
                _context.SaveChanges();
            }
        }

        /// <summary>
        /// Deletes all waitlist entries for a trip.
        /// Called when admin deletes a trip or trip is no longer available.
        /// Returns count of deleted entries for logging/confirmation.
        /// </summary>
        public int DeleteByTripId(int tripId)
        {
            var items = _context.Waitlist.Where(w => w.TripId == tripId).ToList();  // Get all entries
            if (items.Count == 0) return 0;  // Nothing to delete
            
            _context.Waitlist.RemoveRange(items);  // Mark all for deletion
            _context.SaveChanges();                 // Execute DELETE
            return items.Count;  // Return how many deleted
        }

        /// <summary>
        /// Gets a user's active waitlist entries (waiting or recently notified).
        /// 
        /// Active = "Waiting" or "Notified" (excludes "Booked" and "Expired")
        /// Used in user dashboard to show "You're waiting for these trips".
        /// Sorted newest first for better UX.
        /// Include() loads user and trip data for display.
        /// </summary>
        public List<Waitlist> GetActiveByUserId(int userId)
        {
            return _context.Waitlist
                .Include(w => w.User)                                               // Load user data
                .Include(w => w.Trip)                                               // Load trip data
                .Where(w => w.UserId == userId && (w.Status == "Waiting" || w.Status == "Notified"))  // Only active entries
                .OrderByDescending(w => w.CreatedAt)                                // Newest first
                .ToList();
        }

        /// <summary>
        /// Gets all waitlist entries that have expired (24-hour window passed).
        /// 
        /// Expiration logic:
        /// - User was notified (Status = "Notified")
        /// - ExpiresAt was set to CreatedAt + 24 hours
        /// - Current time has passed ExpiresAt
        /// 
        /// Background service periodically calls this to:
        /// 1. Mark these entries as "Expired"
        /// 2. Notify the next person in line
        /// 
        /// Include() loads user and trip data in case we need to log or send follow-up emails.
        /// </summary>
        public List<Waitlist> GetExpiredEntries()
        {
            var now = DateTime.Now;  // Current timestamp
            return _context.Waitlist
                .Include(w => w.User)                   // Load user data
                .Include(w => w.Trip)                   // Load trip data
                .Where(w => w.Status == "Notified" &&   // Was notified
                           w.ExpiresAt != null &&        // Has expiration time set
                           w.ExpiresAt < now)            // Expiration time has passed
                .ToList();
        }

        /// <summary>
        /// Marks a waitlist entry as "Booked" after user successfully completes payment.
        /// 
        /// Called from booking flow:
        /// 1. User receives notification
        /// 2. User adds trip to cart and completes payment
        /// 3. This method marks their waitlist entry as "Booked"
        /// 
        /// This prevents them from being notified again and tracks conversion rate.
        /// </summary>
        public void MarkAsBooked(int userId, int tripId)
        {
            var entry = _context.Waitlist
                .FirstOrDefault(w => w.UserId == userId &&   // Find user's entry
                                    w.TripId == tripId &&    // For this trip
                                    w.Status == "Notified"); // That's currently notified
            if (entry != null)
            {
                entry.Status = "Booked";  // Mark as successfully booked
                _context.SaveChanges();
            }
        }
    }
}
