using Microsoft.AspNetCore.Mvc;
using MVC_project.Data;
using MVC_project.ViewModels;
using System.Collections.Generic;

namespace MVC_project.Controllers
{
    [Route("Filter")]
    public class FilterController : Controller
    {
        private readonly TripRepository _tripRepo;
        private readonly TripImageRepository _imageRepo;
        private readonly TripDateRepository _tripDateRepo;
        private readonly BookingRepository _bookingRepo;

        public FilterController(TripRepository tripRepo, TripImageRepository imageRepo, TripDateRepository tripDateRepo, BookingRepository bookingRepo)
        {
            _tripRepo = tripRepo;
            _imageRepo = imageRepo;
            _tripDateRepo = tripDateRepo;
            _bookingRepo = bookingRepo;
        }

        /// <summary>
        /// Filter and sort trips based on multiple criteria
        /// </summary>
        /// <param name="searchQuery">Universal search across destination, country, description, and package type (partial keyword match)</param>
        /// <param name="destination">Filter by destination name (case-insensitive substring match)</param>
        /// <param name="country">Filter by country name (case-insensitive substring match)</param>
        /// <param name="category">Filter by package type (exact match)</param>
        /// <param name="minPrice">Minimum price filter (considers discount prices)</param>
        /// <param name="maxPrice">Maximum price filter (considers discount prices)</param>
        /// <param name="travelDate">Filter trips starting on or after this date</param>
        /// <param name="discountOnly">Show only discounted trips</param>
        /// <param name="sortBy">Sort option: price-asc, price-desc, date, popular (by completed bookings)</param>
        [HttpGet("Search")]
        public IActionResult Search(
            string searchQuery = "",
            string destination = "",
            string country = "",
            string category = "",
            decimal? minPrice = null,
            decimal? maxPrice = null,
            DateTime? travelDate = null,
            bool discountOnly = false,
            string sortBy = "")
        {
            try
            {
                // Get all active trips
                var trips = _tripRepo.GetActiveTrips().AsEnumerable();

                // Apply universal search query (searches across multiple fields)
                if (!string.IsNullOrWhiteSpace(searchQuery))
                {
                    trips = trips.Where(t => 
                        t.Destination.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                        t.Country.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                        t.Description.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                        t.PackageType.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                    );
                }

                // Apply destination filter
                if (!string.IsNullOrWhiteSpace(destination))
                {
                    trips = trips.Where(t => t.Destination.Contains(destination, StringComparison.OrdinalIgnoreCase));
                }

                // Apply country filter
                if (!string.IsNullOrWhiteSpace(country))
                {
                    trips = trips.Where(t => t.Country.Contains(country, StringComparison.OrdinalIgnoreCase));
                }

                // Apply category filter
                if (!string.IsNullOrWhiteSpace(category))
                {
                    trips = trips.Where(t => t.PackageType == category);
                }

                // Apply minimum price filter (considers discount price if available)
                if (minPrice.HasValue)
                {
                    trips = trips.Where(t => 
                    {
                        var displayPrice = t.DiscountPrice.HasValue && t.DiscountPrice < t.Price 
                            ? t.DiscountPrice.Value 
                            : t.Price;
                        return displayPrice >= minPrice.Value;
                    });
                }

                // Apply maximum price filter (considers discount price if available)
                if (maxPrice.HasValue)
                {
                    trips = trips.Where(t => 
                    {
                        var displayPrice = t.DiscountPrice.HasValue && t.DiscountPrice < t.Price 
                            ? t.DiscountPrice.Value 
                            : t.Price;
                        return displayPrice <= maxPrice.Value;
                    });
                }

                // Apply travel date filter (trips starting on or after the specified date)
                if (travelDate.HasValue)
                {
                    trips = trips.Where(t => t.StartDate.Date >= travelDate.Value.Date);
                }

                // Apply discount filter
                if (discountOnly)
                {
                    trips = trips.Where(t => t.DiscountPrice.HasValue && t.DiscountPrice < t.Price);
                }

                var filteredTrips = trips.ToList();

                // Compute popularity counts (distinct users with completed bookings) once per request
                var popularityCounts = _bookingRepo.GetCompletedDistinctUserCounts(filteredTrips.Select(t => t.TripID));

                // Apply sorting
                var sortedTrips = ApplySorting(filteredTrips, sortBy, popularityCounts);

                // Map to view models with image check
                var tripViewModels = sortedTrips.Select(trip => {
                    var images = _imageRepo.GetByTripId(trip.TripID);
                    var dates = _tripDateRepo.GetByTripId(trip.TripID);
                    
                    return new TripDashboardViewModel
                    {
                        TripID = trip.TripID,
                        Destination = trip.Destination,
                        Country = trip.Country,
                        StartDate = trip.StartDate,
                        EndDate = trip.EndDate,
                        Price = trip.Price,
                        DiscountPrice = trip.DiscountPrice,
                        PackageType = trip.PackageType,
                        Description = trip.Description,
                        AgeLimit = trip.AgeLimit,
                        LatestBookingDate = trip.LatestBookingDate,
                        HasImage = images.Any(),
                        ImageCount = images.Count(),
                        AvailableRooms = trip.AvailableRooms,
                        CancellationEndDate = trip.CancellationEndDate,
                        EffectiveCancellationEndDate = trip.EffectiveCancellationEndDate,
                        DateVariations = dates.Select(d => new TripDateVariation
                        {
                            TripDateID = d.TripDateID,
                            StartDate = d.StartDate,
                            EndDate = d.EndDate,
                            AvailableRooms = d.AvailableRooms
                        }).ToList()
                    };
                }).ToList();

                return View("~/Views/Dashboard/Search.cshtml", tripViewModels);
            }
            catch (Exception ex)
            {
                // Log exception here if you have logging configured
                return BadRequest($"Error filtering trips: {ex.Message}");
            }
        }

        /// <summary>
        /// Get available categories based on current filters (for dynamic dropdown updates)
        /// </summary>
        [HttpGet("GetAvailableCategories")]
        public JsonResult GetAvailableCategories(
            string destination = "",
            string country = "",
            decimal? minPrice = null,
            decimal? maxPrice = null,
            DateTime? travelDate = null,
            bool discountOnly = false)
        {
            try
            {
                var trips = _tripRepo.GetActiveTrips().AsEnumerable();

                // Apply all filters except category
                if (!string.IsNullOrWhiteSpace(destination))
                    trips = trips.Where(t => t.Destination.Contains(destination, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrWhiteSpace(country))
                    trips = trips.Where(t => t.Country.Contains(country, StringComparison.OrdinalIgnoreCase));

                if (minPrice.HasValue)
                {
                    trips = trips.Where(t => 
                    {
                        var displayPrice = t.DiscountPrice.HasValue && t.DiscountPrice < t.Price 
                            ? t.DiscountPrice.Value 
                            : t.Price;
                        return displayPrice >= minPrice.Value;
                    });
                }

                if (maxPrice.HasValue)
                {
                    trips = trips.Where(t => 
                    {
                        var displayPrice = t.DiscountPrice.HasValue && t.DiscountPrice < t.Price 
                            ? t.DiscountPrice.Value 
                            : t.Price;
                        return displayPrice <= maxPrice.Value;
                    });
                }

                if (travelDate.HasValue)
                {
                    trips = trips.Where(t => t.StartDate.Date >= travelDate.Value.Date);
                }

                if (discountOnly)
                {
                    trips = trips.Where(t => t.DiscountPrice.HasValue && t.DiscountPrice < t.Price);
                }

                // Get unique categories from filtered results
                var categories = trips
                    .Select(t => t.PackageType)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList();

                return Json(new { success = true, categories = categories });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Apply sorting to the trips collection
        /// </summary>
        private IEnumerable<MVC_project.Models.Trip> ApplySorting(
            IEnumerable<MVC_project.Models.Trip> trips,
            string sortBy,
            IDictionary<int, int>? popularityCounts = null)
        {
            int PopularityFor(MVC_project.Models.Trip trip)
            {
                if (popularityCounts == null) return 0;
                return popularityCounts.TryGetValue(trip.TripID, out var count) ? count : 0;
            }

            return sortBy switch
            {
                "price-asc" => trips.OrderBy(t => 
                    t.DiscountPrice.HasValue && t.DiscountPrice < t.Price 
                        ? t.DiscountPrice.Value 
                        : t.Price),
                
                "price-desc" => trips.OrderByDescending(t => 
                    t.DiscountPrice.HasValue && t.DiscountPrice < t.Price 
                        ? t.DiscountPrice.Value 
                        : t.Price),
                
                "date" => trips.OrderBy(t => t.StartDate),
                
                "popular" => trips
                    .OrderByDescending(PopularityFor)
                    .ThenBy(t => t.TripID),
                
                _ => trips.OrderBy(t => t.TripID) // Default sorting by ID
            };
        }
    }
}
