using Microsoft.AspNetCore.Mvc;
using MVC_project.Data;
using MVC_project.ViewModels;

namespace MVC_project.Controllers
{
    [Route("Filter")]
    public class FilterController : Controller
    {
        private readonly TripRepository _tripRepo;
        private readonly TripImageRepository _imageRepo;

        public FilterController(TripRepository tripRepo, TripImageRepository imageRepo)
        {
            _tripRepo = tripRepo;
            _imageRepo = imageRepo;
        }

        /// <summary>
        /// Filter and sort trips based on multiple criteria
        /// </summary>
        /// <param name="destination">Filter by destination name (case-insensitive substring match)</param>
        /// <param name="country">Filter by country name (case-insensitive substring match)</param>
        /// <param name="category">Filter by package type (exact match)</param>
        /// <param name="minPrice">Minimum price filter (considers discount prices)</param>
        /// <param name="maxPrice">Maximum price filter (considers discount prices)</param>
        /// <param name="travelDate">Filter trips starting on or after this date</param>
        /// <param name="discountOnly">Show only discounted trips</param>
        /// <param name="sortBy">Sort option: price-asc, price-desc, date, popular</param>
        [HttpGet("Search")]
        public IActionResult Search(
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

                // Apply sorting
                trips = ApplySorting(trips, sortBy);

                // Map to view models with image check
                var tripViewModels = trips.Select(trip => new TripDashboardViewModel
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
                    HasImage = _imageRepo.GetByTripId(trip.TripID).Any()
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
        private IEnumerable<MVC_project.Models.Trip> ApplySorting(IEnumerable<MVC_project.Models.Trip> trips, string sortBy)
        {
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
                
                "popular" => trips.OrderBy(t => Guid.NewGuid()), // Placeholder for future popularity metric
                
                _ => trips.OrderBy(t => t.TripID) // Default sorting by ID
            };
        }
    }
}
