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
        private readonly PasswordService _passwordService;

        public UserController(UserRepository repo, UserTripRepository userTripRepo, TripRepository tripRepo, TripImageRepository imageRepo, PasswordService passwordService)
        {
            _repo = repo;
            _userTripRepo = userTripRepo;
            _tripRepo = tripRepo;
            _imageRepo = imageRepo;
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
            // Get current user's email from claims
            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login", "Login");
            }

            // Get user's trips from database
            var userTrips = _userTripRepo.GetByUserEmail(userEmail);
            
            // Create view models with trip details and images
            var viewModels = userTrips.Select(ut => new UserTripViewModel
            {
                TripID = ut.Trip.TripID,
                Destination = ut.Trip.Destination,
                Country = ut.Trip.Country,
                StartDate = ut.Trip.StartDate,
                EndDate = ut.Trip.EndDate,
                Price = ut.Trip.Price,
                DiscountPrice = ut.Trip.DiscountPrice,
                DiscountEndDate = ut.Trip.DiscountEndDate,
                PackageType = ut.Trip.PackageType,
                AvailableRooms = ut.Trip.AvailableRooms,
                Description = ut.Trip.Description,
                Images = _tripRepo.GetById(ut.TripID) != null 
                    ? _imageRepo.GetByTripId(ut.TripID).Select(img => img.ImageData).ToList()
                    : new List<byte[]>()
            }).ToList();

            return View(viewModels);
        }

        // POST: /User/RemoveFromCart
        [HttpPost]
        [Authorize]
        public IActionResult RemoveFromCart([FromBody] RemoveFromCartRequest request)
        {
            try
            {
                // Get current user's email from claims
                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                // Remove from cart
                bool removed = _userTripRepo.Remove(userEmail, request.TripId);
                
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
                // Get current user's email from claims (stored as Name/NameIdentifier in login)
                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                // Verify trip exists
                var trip = _tripRepo.GetById(request.TripId);
                if (trip == null)
                {
                    return Json(new { success = false, message = "Trip not found" });
                }

                // Add to cart
                bool added = _userTripRepo.Add(userEmail, request.TripId);
                
                if (!added)
                {
                    return Json(new { success = false, message = $"{trip.Destination} is already in your cart!" });
                }

                return Json(new { success = true, message = $"✓ {trip.Destination} added to cart!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while adding to cart" });
            }
        }
    }

    // Request model for AddToCart
    public class AddToCartRequest
    {
        public int TripId { get; set; }
    }

    // Request model for RemoveFromCart
    public class RemoveFromCartRequest
    {
        public int TripId { get; set; }
    }
}
