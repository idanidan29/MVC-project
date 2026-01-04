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
        private readonly BookingRepository _bookingRepo;
        private readonly TripRepository _tripRepo;
        private readonly TripImageRepository _imageRepo;
        private readonly TripDateRepository _dateRepo;
        private readonly PasswordService _passwordService;
        private readonly WaitlistRepository _waitlistRepo;
        private readonly EmailService _emailService;

        public UserController(UserRepository repo, UserTripRepository userTripRepo, BookingRepository bookingRepo, TripRepository tripRepo, TripImageRepository imageRepo, TripDateRepository dateRepo, PasswordService passwordService, WaitlistRepository waitlistRepo, EmailService emailService)
        {
            _repo = repo;
            _userTripRepo = userTripRepo;
            _bookingRepo = bookingRepo;
            _tripRepo = tripRepo;
            _imageRepo = imageRepo;
            _dateRepo = dateRepo;
            _passwordService = passwordService;
            _waitlistRepo = waitlistRepo;
            _emailService = emailService;
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

        // GET: /User/MyBookings
        [Authorize]
        public IActionResult MyBookings()
        {
            // For now, show the same cart view as a placeholder
            // In future, this would show completed bookings/orders
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return RedirectToAction("Login", "Login");
            }

            // Get user's confirmed bookings
            var bookings = _bookingRepo.GetByUserId(userId);

            // Group by TripID to show one card per trip while keeping per-booking date selections
            var groupedTrips = bookings.GroupBy(b => b.TripID).Select(group =>
            {
                var firstBooking = group.First();
                var trip = firstBooking.Trip ?? _tripRepo.GetById(firstBooking.TripID);
                var tripDates = _dateRepo.GetByTripId(firstBooking.TripID);

                return new UserTripViewModel
                {
                    TripID = firstBooking.TripID,
                    Destination = trip?.Destination ?? string.Empty,
                    Country = trip?.Country ?? string.Empty,
                    StartDate = trip?.StartDate ?? DateTime.MinValue,
                    EndDate = trip?.EndDate ?? DateTime.MinValue,
                    Price = firstBooking.UnitPrice,
                    DiscountPrice = null, // bookings store final prices
                    DiscountEndDate = null,
                    PackageType = trip?.PackageType ?? string.Empty,
                    AvailableRooms = trip?.AvailableRooms ?? 0,
                    Description = trip?.Description ?? string.Empty,
                    Quantity = group.Sum(b => b.Quantity),
                    DateVariations = tripDates.Select(td => new DateVariationInfo
                    {
                        StartDate = td.StartDate,
                        EndDate = td.EndDate,
                        AvailableRooms = td.AvailableRooms
                    }).ToList(),
                    UserSelectedDates = group.Select(b => new UserSelectedDateInfo
                    {
                        UserTripID = b.BookingID,
                        SelectedDateIndex = b.SelectedDateIndex,
                        Quantity = b.Quantity,
                        Status = b.Status,
                        BookingDate = b.BookingDate,
                        UnitPrice = b.UnitPrice,
                        TotalPrice = b.TotalPrice
                    }).ToList(),
                    Images = trip != null
                        ? _imageRepo.GetByTripId(trip.TripID).Select(img => img.ImageData).ToList()
                        : new List<byte[]>()
                };
            }).ToList();

            return View(groupedTrips);
        }

        // GET: /User/Cart
        [Authorize]
        public IActionResult Cart()
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
        public async Task<IActionResult> RemoveFromCart([FromBody] RemoveFromCartRequest request)
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
                var removedItem = _userTripRepo.RemoveByUserTripId(request.UserTripID);
                
                if (removedItem == null)
                {
                    return Json(new { success = false, message = "Trip not found in cart" });
                }

                // Restore available rooms
                if (removedItem.Trip != null)
                {
                    removedItem.Trip.AvailableRooms += removedItem.Quantity;
                    _tripRepo.Update(removedItem.Trip);
                    
                    // Process waitlist - notify next users
                    await ProcessWaitlistForTrip(removedItem.TripID, removedItem.Quantity);
                }

                return Json(new { success = true, message = "Trip removed from cart", tripId = removedItem.TripID, availableRooms = removedItem.Trip?.AvailableRooms ?? 0 });
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
                
                // Check if no rooms available - add to waitlist
                if (trip.AvailableRooms == 0)
                {
                    // Check if user is already on waitlist
                    if (_waitlistRepo.IsUserOnWaitlist(userId, request.TripId))
                    {
                        return Json(new { success = false, onWaitlist = true, message = $"You are already on the waitlist for {trip.Destination}. We'll notify you when a room becomes available!" });
                    }

                    // Add to waitlist
                    var addedToWaitlist = _waitlistRepo.AddToWaitlist(userId, request.TripId);
                    if (addedToWaitlist)
                    {
                        return Json(new { success = true, onWaitlist = true, message = $"No rooms available for {trip.Destination}. You've been added to the waitlist and will be notified via email when a room opens up!" });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Failed to add to waitlist." });
                    }
                }
                
                // Check if enough rooms available for the NEW quantity being added
                // (existing quantity already deducted from AvailableRooms)
                if (qty > trip.AvailableRooms)
                {
                    return Json(new { success = false, message = $"Only {trip.AvailableRooms} room(s) available for {trip.Destination}." });
                }

                // Add to cart with quantity and selected date (increments if existing)
                bool added = _userTripRepo.Add(userId, request.TripId, qty, request.SelectedDateIndex);
                
                if (!added)
                {
                    return Json(new { success = false, message = $"{trip.Destination} is already in your cart!" });
                }

                // Decrease available rooms (reserve them in cart)
                trip.AvailableRooms -= qty;
                if (trip.AvailableRooms < 0) trip.AvailableRooms = 0;
                _tripRepo.Update(trip);

                return Json(new { success = true, message = $"✓ {trip.Destination} added to cart (x{qty})!", tripId = request.TripId, availableRooms = trip.AvailableRooms });
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

        // Process waitlist when rooms become available
        private async Task ProcessWaitlistForTrip(int tripId, int roomsFreed)
        {
            try
            {
                var trip = _tripRepo.GetById(tripId);
                if (trip == null) return;

                // Process waitlist users one by one until no more rooms or no more waiting users
                while (roomsFreed > 0 && trip.AvailableRooms > 0)
                {
                    var nextWaitlistUser = _waitlistRepo.GetNextWaitingUser(tripId);
                    if (nextWaitlistUser == null) break; // No more users waiting

                    // Add trip to their cart (1 room per waitlist user)
                    _userTripRepo.Add(nextWaitlistUser.UserId, tripId, 1);

                    // Decrease available rooms
                    trip.AvailableRooms -= 1;
                    _tripRepo.Update(trip);

                    // Mark as notified and set email sent time
                    _waitlistRepo.MarkEmailSent(nextWaitlistUser.WaitlistID);
                    nextWaitlistUser.Status = "Notified";
                    
                    // Send email notification
                    await _emailService.SendWaitlistNotificationAsync(
                        nextWaitlistUser.User!.email,
                        nextWaitlistUser.User.first_name,
                        trip.Destination
                    );

                    roomsFreed--;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing waitlist: {ex.Message}");
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
