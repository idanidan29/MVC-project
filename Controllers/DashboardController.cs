using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MVC_project.Data;
using MVC_project.ViewModels;
using System.Security.Claims;

namespace MVC_project.Controllers
{
    [Route("Dashboard")]
    public class DashboardController : Controller
    {
        private readonly TripRepository _tripRepo;
        private readonly TripImageRepository _imageRepo;
        private readonly TripDateRepository _tripDateRepo;
        private readonly TripRatingRepository _ratingRepo;
        private readonly BookingRepository _bookingRepo;

        public DashboardController(TripRepository tripRepo, TripImageRepository imageRepo, TripDateRepository tripDateRepo, TripRatingRepository ratingRepo, BookingRepository bookingRepo)
        {
            _tripRepo = tripRepo;
            _imageRepo = imageRepo;
            _tripDateRepo = tripDateRepo;
            _ratingRepo = ratingRepo;
            _bookingRepo = bookingRepo;
        }

        // GET: /Dashboard or /Dashboard/Index
        [HttpGet("")]
        [HttpGet("Index")]
        public IActionResult Index()
        {
            // Get trips - all for admins, only active and visible for regular users
            var trips = User?.IsInRole("Admin") == true 
                ? _tripRepo.GetAll().Where(t => t.IsActive)
                : _tripRepo.GetAll().Where(t => t.IsActive && t.IsVisible);

            // Map to view models with image check
            var tripViewModels = trips.Select(trip => {
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
                    PreviousPrice = trip.PreviousPrice,
                    PriceChangedAt = trip.PriceChangedAt,
                    PackageType = trip.PackageType,
                    Description = trip.Description,
                    AgeLimit = trip.AgeLimit,
                    LatestBookingDate = trip.LatestBookingDate,
                    HasImage = images.Any(),
                    ImageCount = images.Count(),
                    AvailableRooms = trip.AvailableRooms,
                    IsVisible = trip.IsVisible,
                                        RatingSum = trip.RatingSum,
                                        RatingCount = trip.RatingCount,
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

            return View(tripViewModels);
        }

        // GET: /Dashboard/Search - displays the search page
        [HttpGet("Search")]
        public IActionResult Search()
        {
            // Redirect to filter controller for actual filtering
            // This action just loads the initial view
            return RedirectToAction("Search", "Filter");
        }

        // GET: /Dashboard/GetImage?tripId=1
        [HttpGet("GetImage")]
        public IActionResult GetImage(int tripId)
        {
            // Get first image for the trip
            var image = _imageRepo.GetByTripId(tripId).FirstOrDefault();
            
            if (image == null)
                return NotFound();

            return File(image.ImageData, image.ContentType ?? "image/jpeg");
        }

        // GET: /Dashboard/GetAllImages?tripId=1
        [HttpGet("GetAllImages")]
        public IActionResult GetAllImages(int tripId)
        {
            var images = _imageRepo.GetByTripId(tripId);
            
            if (!images.Any())
                return Ok(new { images = new List<object>() });

            var imageList = images.Select(img => new
            {
                imageId = img.ImageID,
                imageUrl = Url.Action("GetImageById", "Dashboard", new { imageId = img.ImageID })
            }).ToList();

            return Ok(new { images = imageList });
        }

        // GET: /Dashboard/GetImageById?imageId=1
        [HttpGet("GetImageById")]
        public IActionResult GetImageById(int imageId)
        {
            var image = _imageRepo.GetById(imageId);
            
            if (image == null)
                return NotFound();

            return File(image.ImageData, image.ContentType ?? "image/jpeg");
        }

        // POST: /Dashboard/SubmitRating
        [HttpPost("SubmitRating")]
        public IActionResult SubmitRating([FromBody] RatingSubmission submission)
        {
            if (!User.Identity?.IsAuthenticated ?? true)
                return Unauthorized(new { success = false, message = "You must be logged in to rate." });

            if (submission.rating < 1 || submission.rating > 5)
                return BadRequest(new { success = false, message = "Rating must be between 1 and 5." });

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { success = false, message = "User not found." });

            // Check if user has booked this trip
            var hasBooked = _bookingRepo.GetByUserId(userId)
                .Any(b => b.TripID == submission.tripId && (b.Status ?? string.Empty) != "Cancelled");

            if (!hasBooked)
                return BadRequest(new { success = false, message = "You can only rate trips you have booked." });

            try
            {
                _ratingRepo.AddOrUpdateRating(submission.tripId, userId, submission.rating, submission.comment);
                return Ok(new { success = true, message = "Rating submitted successfully!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // GET: /Dashboard/GetTripRatings?tripId=1
        [HttpGet("GetTripRatings")]
        public IActionResult GetTripRatings(int tripId)
        {
            var ratings = _ratingRepo.GetByTripId(tripId);
            var trip = _tripRepo.GetById(tripId);

            if (trip == null)
                return NotFound();

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(userIdClaim, out var userId);

            var userRating = userId > 0 ? _ratingRepo.GetUserRating(tripId, userId) : null;
            var hasBooked = userId > 0 ? _bookingRepo.GetByUserId(userId)
                .Any(b => b.TripID == tripId && (b.Status ?? string.Empty) != "Cancelled") : false;

            return Ok(new
            {
                trip = new
                {
                    ratingSum = trip.RatingSum,
                    ratingCount = trip.RatingCount,
                    averageRating = trip.RatingCount > 0 ? trip.RatingSum / trip.RatingCount : 0
                },
                userRating = userRating == null ? null : new
                {
                    rating = userRating.Rating,
                    comment = userRating.Comment,
                    createdAt = userRating.CreatedAt
                },
                hasBooked = hasBooked,
                isAuthenticated = User.Identity?.IsAuthenticated ?? false,
                ratings = ratings.Select(r => new
                {
                    userName = $"{r.User?.first_name ?? "User"} {r.User?.last_name ?? ""}".Trim(),
                    rating = r.Rating,
                    comment = r.Comment,
                    createdAt = r.CreatedAt.ToString("MMM dd, yyyy")
                }).ToList()
            });
        }
    }
}
