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
                .Count(b => b.UserId == userId && b.Trip != null && b.Trip.EndDate >= todayUtc.Date);
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
    }
}
