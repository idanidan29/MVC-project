using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MVC_project.Models;
using MVC_project.ViewModels;
using MVC_project.Data;
using MVC_project.Services;
using System.Security.Claims;

namespace MVC_project.Controllers
{
    public class UserController : Controller
    {
        private readonly UserRepository _repo;
        private readonly UserTripRepository _userTripRepo;
        private readonly TripRepository _tripRepo;
        private readonly TripImageRepository _imageRepo;
        private readonly TripDateRepository _dateRepo;
        private readonly PasswordService _passwordService;

        public UserController(UserRepository repo, UserTripRepository userTripRepo, TripRepository tripRepo, TripImageRepository imageRepo, TripDateRepository dateRepo, PasswordService passwordService)
        {
            _repo = repo;
            _userTripRepo = userTripRepo;
            _tripRepo = tripRepo;
            _imageRepo = imageRepo;
            _dateRepo = dateRepo;
            _passwordService = passwordService;
        }

        // GET: /User/Register
        public IActionResult Register()
        {
            // MVC will automatically look for Views/User/Register.cshtml
            return View();
        }

        // POST: /User/Register
        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Pass the model back to the same view in case of errors
                return View(model);
            }

            if (_repo.EmailExists(model.Email))
            {
                ModelState.AddModelError("Email", "This email address is already registered");
                return View(model);
            }

            var user = new User
            {
                first_name = model.FirstName,
                last_name = model.LastName,
                email = model.Email,
                passwordHash = _passwordService.HashPassword(model.Password),
                admin = false
            };

            _repo.Add(user);

            // Redirect to Login page after successful registration
            return RedirectToAction("Login", "Login"); // Specify controller if Login is in another controller
        }

        // GET: /User/Bookings
        [Authorize]
        public IActionResult Bookings()
        {
            // Get current user's ID from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return RedirectToAction("Login", "Login");
            }

            // Get user's trips from database
            var userTrips = _userTripRepo.GetByUserId(userId);
            
            // Group by TripID to show one card per trip
            var groupedTrips = userTrips.GroupBy(ut => ut.TripID).Select(group =>
            {
                var firstTrip = group.First();
                var tripDates = _dateRepo.GetByTripId(firstTrip.TripID);
                
                return new UserTripViewModel
                {
                    TripID = firstTrip.Trip.TripID,
                    Destination = firstTrip.Trip.Destination,
                    Country = firstTrip.Trip.Country,
                    StartDate = firstTrip.Trip.StartDate,
                    EndDate = firstTrip.Trip.EndDate,
                    Price = firstTrip.Trip.Price,
                    DiscountPrice = firstTrip.Trip.DiscountPrice,
                    DiscountEndDate = firstTrip.Trip.DiscountEndDate,
                    PackageType = firstTrip.Trip.PackageType,
                    AvailableRooms = firstTrip.Trip.AvailableRooms,
                    Description = firstTrip.Trip.Description,
                    Quantity = group.Sum(ut => ut.Quantity),  // Total quantity across all dates
                    DateVariations = tripDates.Select(td => new DateVariationInfo
                    {
                        StartDate = td.StartDate,
                        EndDate = td.EndDate,
                        AvailableRooms = td.AvailableRooms
                    }).ToList(),
                    // List of all user's selected dates for this trip
                    UserSelectedDates = group.Select(ut => new UserSelectedDateInfo
                    {
                        UserTripID = ut.UserTripID,
                        SelectedDateIndex = ut.SelectedDateIndex,
                        Quantity = ut.Quantity
                    }).ToList(),
                    Images = _tripRepo.GetById(firstTrip.TripID) != null 
                        ? _imageRepo.GetByTripId(firstTrip.TripID).Select(img => img.ImageData).ToList()
                        : new List<byte[]>()
                };
            }).ToList();

            return View(groupedTrips);
        }

        // POST: /User/RemoveFromCart
        [HttpPost]
        [Authorize]
        public IActionResult RemoveFromCart([FromBody] RemoveFromCartRequest request)
        {
            try
            {
                // Get current user's ID from claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                // Remove specific cart entry by UserTripID
                bool removed = _userTripRepo.RemoveByUserTripId(request.UserTripID);
                
                if (!removed)
                {
                    return Json(new { success = false, message = "Trip not found in cart" });
                }

                return Json(new { success = true, message = "Trip removed from cart" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while removing from cart" });
            }
        }

        // POST: /User/AddToCart
        [HttpPost]
        [Authorize]
        public IActionResult AddToCart([FromBody] AddToCartRequest request)
        {
            try
            {
                // Get current user's ID from claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                // Verify trip exists
                var trip = _tripRepo.GetById(request.TripId);
                if (trip == null)
                {
                    return Json(new { success = false, message = "Trip not found" });
                }

                // Validate quantity
                var qty = request.Quantity <= 0 ? 1 : request.Quantity;
                
                // Check if trip is already in cart and calculate total quantity
                var existingInCart = _userTripRepo.GetByUserId(userId)
                    .FirstOrDefault(ut => ut.TripID == request.TripId);
                var totalQty = existingInCart != null ? existingInCart.Quantity + qty : qty;
                
                if (totalQty > trip.AvailableRooms)
                {
                    return Json(new { success = false, message = $"Only {trip.AvailableRooms} rooms available for {trip.Destination}. You already have {existingInCart?.Quantity ?? 0} in your cart." });
                }

                // Add to cart with quantity and selected date (increments if existing)
                bool added = _userTripRepo.Add(userId, request.TripId, qty, request.SelectedDateIndex);
                
                if (!added)
                {
                    return Json(new { success = false, message = $"{trip.Destination} is already in your cart!" });
                }

                return Json(new { success = true, message = $"✓ {trip.Destination} added to cart (x{qty})!" });
            }
            catch (Exception ex)
            {
                // Log the actual error for debugging
                var innerMessage = ex.InnerException?.Message ?? "No inner exception";
                Console.WriteLine($"AddToCart Error: {ex.Message}");
                Console.WriteLine($"Inner Exception: {innerMessage}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return Json(new { success = false, message = $"Database error: {innerMessage}" });
            }
        }
    }

    // Request model for AddToCart
    public class AddToCartRequest
    {
        public int TripId { get; set; }
        public int Quantity { get; set; }
        public int SelectedDateIndex { get; set; } = -1; // -1 for main date, 0+ for variations
    }

    // Request model for RemoveFromCart
    public class RemoveFromCartRequest
    {
        public int UserTripID { get; set; }  // Specific cart entry ID
    }
}
