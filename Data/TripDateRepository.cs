using MVC_project.Models;
using Microsoft.EntityFrameworkCore;

namespace MVC_project.Data
{
    public class TripDateRepository
    {
        private readonly AppDbContext _context;

        public TripDateRepository(AppDbContext context)
        {
            _context = context;
        }

        // Add a single trip date
        public void Add(TripDate tripDate)
        {
            _context.TripDates.Add(tripDate);
            _context.SaveChanges();
        }

        // Add multiple trip dates
        public void AddRange(IEnumerable<TripDate> tripDates)
        {
            _context.TripDates.AddRange(tripDates);
            _context.SaveChanges();
        }

        // Get all dates for a specific trip
        public IEnumerable<TripDate> GetByTripId(int tripId)
        {
            return _context.TripDates
                .Where(td => td.TripID == tripId)
                .OrderBy(td => td.StartDate)
                .ToList();
        }

        // Get a specific trip date by ID
        public TripDate? GetById(int tripDateId)
        {
            return _context.TripDates.FirstOrDefault(td => td.TripDateID == tripDateId);
        }

        // Update a trip date
        public void Update(TripDate tripDate)
        {
            _context.TripDates.Update(tripDate);
            _context.SaveChanges();
        }

        // Delete a trip date
        public void Delete(int tripDateId)
        {
            var tripDate = GetById(tripDateId);
            if (tripDate != null)
            {
                _context.TripDates.Remove(tripDate);
                _context.SaveChanges();
            }
        }

        // Delete all dates for a trip
        public void DeleteByTripId(int tripId)
        {
            var tripDates = GetByTripId(tripId);
            _context.TripDates.RemoveRange(tripDates);
            _context.SaveChanges();
        }

        // Check if any dates exist for a trip
        public bool HasDates(int tripId)
        {
            return _context.TripDates.Any(td => td.TripID == tripId);
        }

        // Get count of date variations for a trip
        public int GetCount(int tripId)
        {
            return _context.TripDates.Count(td => td.TripID == tripId);
        }
    }
}
