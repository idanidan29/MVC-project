using MVC_project.Models;
using Microsoft.EntityFrameworkCore;

namespace MVC_project.Data
{
    /// <summary>
    /// Repository for TripDate entity operations.
    /// Manages alternative date options for trips. Each trip can have multiple start/end date combinations
    /// allowing customers to choose from different departure dates for the same trip package.
    /// </summary>
    public class TripDateRepository
    {
        private readonly AppDbContext _context;

        public TripDateRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Adds a single alternative date to a trip.
        /// Each TripDate represents one possible departure/return date combination.
        /// </summary>
        public void Add(TripDate tripDate)
        {
            _context.TripDates.Add(tripDate);  // Add to change tracker
            _context.SaveChanges();             // Persist to database
        }

        /// <summary>
        /// Adds multiple alternative dates in one transaction.
        /// More efficient than individual Add() calls when admin creates multiple date options.
        /// Example: A trip might offer weekly departures for the next 3 months.
        /// </summary>
        public void AddRange(IEnumerable<TripDate> tripDates)
        {
            _context.TripDates.AddRange(tripDates);  // Add all dates to change tracker
            _context.SaveChanges();                   // Single database transaction
        }

        /// <summary>
        /// Retrieves all alternative dates for a trip, sorted by start date.
        /// OrderBy ensures dates are displayed chronologically to customers.
        /// </summary>
        public IEnumerable<TripDate> GetByTripId(int tripId)
        {
            return _context.TripDates
                .Where(td => td.TripID == tripId)  // Filter to specified trip
                .OrderBy(td => td.StartDate)        // Sort earliest to latest (SQL: ORDER BY StartDate)
                .ToList();                          // Execute query and materialize
        }

        /// <summary>
        /// Retrieves a specific trip date option by its ID.
        /// Used when customer selects a specific date from available options.
        /// </summary>
        public TripDate? GetById(int tripDateId)
        {
            return _context.TripDates.FirstOrDefault(td => td.TripDateID == tripDateId);  // Find by primary key
        }

        /// <summary>
        /// Updates an existing trip date.
        /// Admin can modify start/end dates or availability for a date option.
        /// </summary>
        public void Update(TripDate tripDate)
        {
            _context.TripDates.Update(tripDate);  // Mark as modified in change tracker
            _context.SaveChanges();                // Persist changes to database
        }

        /// <summary>
        /// Deletes a specific trip date option.
        /// Admin removes this date if it's no longer available or was added by mistake.
        /// </summary>
        public void Delete(int tripDateId)
        {
            var tripDate = GetById(tripDateId);  // Retrieve entity
            if (tripDate != null)                 // Verify it exists
            {
                _context.TripDates.Remove(tripDate);  // Mark for deletion
                _context.SaveChanges();                // Execute DELETE
            }
        }

        /// <summary>
        /// Deletes all alternative dates for a trip.
        /// Called when admin deletes the entire trip or resets all date options.
        /// </summary>
        public void DeleteByTripId(int tripId)
        {
            var tripDates = GetByTripId(tripId);       // Get all dates for trip
            _context.TripDates.RemoveRange(tripDates);  // Mark all for deletion
            _context.SaveChanges();                     // Delete in single transaction
        }

        /// <summary>
        /// Checks if a trip has any alternative dates configured.
        /// Any() is more efficient than Count() when we only need a boolean result.
        /// </summary>
        public bool HasDates(int tripId)
        {
            return _context.TripDates.Any(td => td.TripID == tripId);  // Returns true if at least one exists (stops at first match)
        }

        /// <summary>
        /// Gets the count of alternative date options for a trip.
        /// Used in admin dashboard to show "Trip has 5 date options available".
        /// </summary>
        public int GetCount(int tripId)
        {
            return _context.TripDates.Count(td => td.TripID == tripId);  // SQL: SELECT COUNT(*) WHERE TripID = @tripId
        }
    }
}
