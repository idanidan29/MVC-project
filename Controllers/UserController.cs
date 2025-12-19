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
        private readonly PasswordService _passwordService;

        public UserController(UserRepository repo, UserTripRepository userTripRepo, TripRepository tripRepo, PasswordService passwordService)
        {
            _repo = repo;
            _userTripRepo = userTripRepo;
            _tripRepo = tripRepo;
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
}
