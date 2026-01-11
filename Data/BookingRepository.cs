using System;
using Microsoft.EntityFrameworkCore;
using MVC_project.Models;

namespace MVC_project.Data
{
    public class BookingRepository
    {
        private readonly AppDbContext _context;

        public BookingRepository(AppDbContext context)
        {
            _context = context;
        }

        public Booking Add(Booking booking)
        {
            _context.Bookings.Add(booking);
            _context.SaveChanges();
            return booking;
        }

        public void Update(Booking booking)
        {
            _context.Bookings.Update(booking);
            _context.SaveChanges();
        }

        public IEnumerable<Booking> GetByUserId(int userId)
        {
            return _context.Bookings
                .Include(b => b.Trip)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.BookingDate)
                .ToList();
        }

        public IEnumerable<Booking> GetActiveByUserId(int userId)
        {
            return _context.Bookings
                .Include(b => b.Trip)
                .Where(b => b.UserId == userId && b.Status != "Cancelled")
                .OrderByDescending(b => b.BookingDate)
                .ToList();
        }

        public int CountActiveByUserId(int userId)
        {
            return _context.Bookings
                .Where(b => b.UserId == userId && (b.Status ?? string.Empty) != "Cancelled")
                .Select(b => b.TripID)
                .Distinct()
                .Count();
        }

        public int CountUpcoming(int userId, DateTime todayUtc)
        {
            return _context.Bookings
                .Include(b => b.Trip)
                .Count(b => b.UserId == userId 
                    && b.Trip != null 
                    && b.Trip.EndDate >= todayUtc.Date
                    && (b.Status ?? string.Empty) == "Confirmed");
        }

        public Dictionary<int, int> GetCompletedBookingCounts(IEnumerable<int> tripIds)
        {
            var ids = tripIds?.Distinct().ToList();
            if (ids == null || ids.Count == 0)
                return new Dictionary<int, int>();

            var completedStatuses = new[] { "Confirmed", "Booked" };

            return _context.Bookings
                .Where(b => ids.Contains(b.TripID)
                    && completedStatuses.Contains((b.Status ?? string.Empty)))
                .GroupBy(b => b.TripID)
                .Select(g => new { TripId = g.Key, Count = g.Count() })
                .ToDictionary(x => x.TripId, x => x.Count);
        }

        public Dictionary<int, int> GetCompletedDistinctUserCounts(IEnumerable<int> tripIds)
        {
            var ids = tripIds?.Distinct().ToList();
            if (ids == null || ids.Count == 0)
                return new Dictionary<int, int>();

            var completedStatuses = new[] { "Confirmed", "Booked" };

            return _context.Bookings
                .Where(b => ids.Contains(b.TripID)
                    && completedStatuses.Contains((b.Status ?? string.Empty)))
                .GroupBy(b => b.TripID)
                .Select(g => new { TripId = g.Key, Count = g.Select(b => b.UserId).Distinct().Count() })
                .ToDictionary(x => x.TripId, x => x.Count);
        }

        public Booking? GetByIdForUser(int bookingId, int userId)
        {
            return _context.Bookings
                .Include(b => b.Trip)
                .Include(b => b.User)
                .FirstOrDefault(b => b.BookingID == bookingId && b.UserId == userId);
        }

        /// <summary>
        /// Determines if a booking can be cancelled by the user at the given time.
        /// Cancellation is allowed only if the booking is not already marked as "Cancelled"
        /// and the current date is on or before the trip's effective cancellation deadline.
        /// </summary>
        public bool CanCancelBooking(int bookingId, int userId, DateTime utcNow)
        {
            var booking = _context.Bookings
                .Include(b => b.Trip)
                .FirstOrDefault(b => b.BookingID == bookingId && b.UserId == userId);

            if (booking == null || booking.Trip == null)
                return false;

            var status = booking.Status ?? string.Empty;
            if (status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
                return false;

            var today = utcNow.Date;
            return today <= booking.Trip.EffectiveCancellationEndDate.Date;
        }

        // Delete all bookings associated with a specific trip
        public int DeleteByTripId(int tripId)
        {
            var bookings = _context.Bookings.Where(b => b.TripID == tripId).ToList();
            if (bookings.Count == 0) return 0;
            _context.Bookings.RemoveRange(bookings);
            _context.SaveChanges();
            return bookings.Count;
        }
    }
}
