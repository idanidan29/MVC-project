using MVC_project.Models;
using Microsoft.EntityFrameworkCore;

namespace MVC_project.Data
{
    public class WaitlistRepository
    {
        private readonly AppDbContext _context;

        public WaitlistRepository(AppDbContext context)
        {
            _context = context;
        }

        // Check if user is already on waitlist for a specific trip
        public bool IsUserOnWaitlist(int userId, int tripId)
        {
            return _context.Waitlist
                .Any(w => w.UserId == userId && w.TripId == tripId && w.Status == "Waiting");
        }

        // Add user to waitlist
        public bool AddToWaitlist(int userId, int tripId)
        {
            // Check if already on waitlist
            if (IsUserOnWaitlist(userId, tripId))
            {
                return false;
            }

            var waitlistEntry = new Waitlist
            {
                UserId = userId,
                TripId = tripId,
                Status = "Waiting",
                CreatedAt = DateTime.Now,
                ExpiresAt = null,
                EmailSentAt = null
            };

            _context.Waitlist.Add(waitlistEntry);
            _context.SaveChanges();
            return true;
        }

        // Get all users who need to be notified (Status = Notified but EmailSentAt is null)
        public List<Waitlist> GetPendingNotifications()
        {
            return _context.Waitlist
                .Include(w => w.User)
                .Include(w => w.Trip)
                .Where(w => w.Status == "Notified" && w.EmailSentAt == null)
                .ToList();
        }

        // Update EmailSentAt after sending notification
        public void MarkEmailSent(int waitlistId)
        {
            var entry = _context.Waitlist.Find(waitlistId);
            if (entry != null)
            {
                entry.EmailSentAt = DateTime.Now;
                entry.ExpiresAt = DateTime.Now.AddHours(24); // User has 24 hours to book
                entry.Status = "Notified"; // Update status
                _context.SaveChanges();
            }
        }

        // Update status
        public void UpdateStatus(int waitlistId, string status)
        {
            var entry = _context.Waitlist.Find(waitlistId);
            if (entry != null)
            {
                entry.Status = status;
                _context.SaveChanges();
            }
        }

        // Get next waiting user for a trip (FIFO)
        public Waitlist? GetNextWaitingUser(int tripId)
        {
            return _context.Waitlist
                .Include(w => w.User)
                .Include(w => w.Trip)
                .Where(w => w.TripId == tripId && w.Status == "Waiting")
                .OrderBy(w => w.CreatedAt)
                .FirstOrDefault();
        }

        // Notify next user in line when a room becomes available
        public void NotifyNextUser(int tripId)
        {
            var nextUser = GetNextWaitingUser(tripId);
            if (nextUser != null)
            {
                nextUser.Status = "Notified";
                _context.SaveChanges();
            }
        }

        // Get all expired notified entries (24 hours passed since notification)
        public List<Waitlist> GetExpiredEntries()
        {
            var now = DateTime.Now;
            return _context.Waitlist
                .Include(w => w.User)
                .Include(w => w.Trip)
                .Where(w => w.Status == "Notified" && 
                           w.ExpiresAt != null && 
                           w.ExpiresAt < now)
                .ToList();
        }

        // Mark waitlist entry as booked (called after successful payment)
        public void MarkAsBooked(int userId, int tripId)
        {
            var entry = _context.Waitlist
                .FirstOrDefault(w => w.UserId == userId && 
                                    w.TripId == tripId && 
                                    w.Status == "Notified");
            if (entry != null)
            {
                entry.Status = "Booked";
                _context.SaveChanges();
            }
        }
    }
}
