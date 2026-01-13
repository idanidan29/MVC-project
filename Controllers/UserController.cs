using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MVC_project.Models;
using MVC_project.ViewModels;
using MVC_project.Data;
using MVC_project.Services;
using System.Security.Claims;
using System.Text;
using System.Collections.Generic;

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

            // Get ALL user bookings (confirmed, cancelled, past)
            var bookings = _bookingRepo.GetByUserId(userId);

            // Group by TripID to show one card per trip while keeping per-booking date selections
            var groupedTrips = bookings.GroupBy(b => b.TripID).Select(group =>
            {
                var firstBooking = group.First();
                var trip = firstBooking.Trip ?? _tripRepo.GetById(firstBooking.TripID);
                var tripDates = _dateRepo.GetByTripId(firstBooking.TripID);

                // Determine booking status: Cancelled, Past, or Confirmed
                var bookingStatus = firstBooking.Status?.ToLower() == "cancelled" ? "Cancelled" :
                                   (trip?.EndDate < DateTime.UtcNow ? "Past" : "Confirmed");

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
                    CancellationEndDate = trip?.CancellationEndDate,
                    EffectiveCancellationEndDate = trip?.EffectiveCancellationEndDate ?? DateTime.MinValue,
                    Status = bookingStatus,
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
                    CancellationEndDate = firstTrip.Trip.CancellationEndDate,
                    EffectiveCancellationEndDate = firstTrip.Trip.EffectiveCancellationEndDate,
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
                Console.WriteLine($"RemoveFromCart error: {ex.Message}");
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
                
                // Determine which date's rooms to check and decrease
                int availableRooms;
                TripDate? selectedTripDate = null;
                
                if (request.SelectedDateIndex >= 0)
                {
                    // User selected an alternative date
                    var tripDates = _dateRepo.GetByTripId(request.TripId).ToList();
                    if (request.SelectedDateIndex < tripDates.Count)
                    {
                        selectedTripDate = tripDates[request.SelectedDateIndex];
                        availableRooms = selectedTripDate.AvailableRooms;
                    }
                    else
                    {
                        return Json(new { success = false, message = "Selected date is not valid." });
                    }
                }
                else
                {
                    // User selected main trip date (-1)
                    availableRooms = trip.AvailableRooms;
                }
                
                // Check if no rooms available - prompt to join waitlist instead of auto-adding
                if (availableRooms == 0)
                {
                    var currentCount = _waitlistRepo.GetWaitlistCountForTrip(request.TripId);

                    // Check if user is already on waitlist
                    if (_waitlistRepo.IsUserOnWaitlist(userId, request.TripId))
                    {
                        return Json(new { success = false, onWaitlist = true, waitlistCount = currentCount, message = $"You are already on the waitlist for {trip.Destination}. We'll notify you when a room becomes available!" });
                    }

                    // Ask client to show a modal prompt to join waitlist
                    return Json(new { success = false, showWaitlistPrompt = true, waitlistCount = currentCount, tripId = request.TripId, destination = trip.Destination });
                }
                
                // Check if enough rooms available for the NEW quantity being added
                if (qty > availableRooms)
                {
                    return Json(new { success = false, message = $"Only {availableRooms} room(s) available for this date." });
                }

                // Add to cart with quantity and selected date (increments if existing)
                bool added = _userTripRepo.Add(userId, request.TripId, qty, request.SelectedDateIndex);
                
                if (!added)
                {
                    return Json(new { success = false, message = $"{trip.Destination} is already in your cart!" });
                }

                // Decrease available rooms for the correct date
                if (selectedTripDate != null)
                {
                    // Decrease alternative date rooms
                    selectedTripDate.AvailableRooms -= qty;
                    if (selectedTripDate.AvailableRooms < 0) selectedTripDate.AvailableRooms = 0;
                    _dateRepo.Update(selectedTripDate);
                }
                else
                {
                    // Decrease main trip date rooms
                    trip.AvailableRooms -= qty;
                    if (trip.AvailableRooms < 0) trip.AvailableRooms = 0;
                    _tripRepo.Update(trip);
                }

                return Json(new { success = true, message = $"✓ {trip.Destination} added to cart (x{qty})!", tripId = request.TripId, selectedDateIndex = request.SelectedDateIndex, availableRooms = (selectedTripDate?.AvailableRooms ?? trip.AvailableRooms) });
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

        [Authorize]
        [HttpGet]
        public IActionResult Itinerary(int bookingId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized();
            }

            var booking = _bookingRepo.GetByIdForUser(bookingId, userId);
            if (booking == null)
            {
                return NotFound();
            }

            var trip = booking.Trip ?? _tripRepo.GetById(booking.TripID);
            var dates = ResolveBookingDates(booking);

            return Json(new
            {
                destination = trip?.Destination ?? "",
                country = trip?.Country ?? "",
                startDate = dates.Start.ToString("MMM dd, yyyy"),
                endDate = dates.End.ToString("MMM dd, yyyy"),
                durationDays = (dates.End - dates.Start).Days,
                travelers = booking.Quantity,
                packageType = trip?.PackageType ?? "",
                status = booking.Status,
                bookedOn = booking.BookingDate.ToString("MMM dd, yyyy"),
                unitPrice = booking.UnitPrice,
                totalPrice = booking.TotalPrice,
                description = trip?.Description ?? "No description available"
            });
        }

        // POST: /User/AddToWaitlist
        [HttpPost]
        [Authorize]
        public IActionResult AddToWaitlist([FromBody] WaitlistRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var trip = _tripRepo.GetById(request.TripId);
                if (trip == null)
                {
                    return Json(new { success = false, message = "Trip not found" });
                }

                if (_waitlistRepo.IsUserOnWaitlist(userId, request.TripId))
                {
                    var countExisting = _waitlistRepo.GetWaitlistCountForTrip(request.TripId);
                    return Json(new { success = false, onWaitlist = true, waitlistCount = countExisting, message = $"You are already on the waitlist for {trip.Destination}." });
                }

                var added = _waitlistRepo.AddToWaitlist(userId, request.TripId);
                if (added)
                {
                    var countAfter = _waitlistRepo.GetWaitlistCountForTrip(request.TripId);
                    return Json(new { success = true, onWaitlist = true, waitlistCount = countAfter, message = $"You're on the waitlist for {trip.Destination}. We'll notify you when a room opens up!" });
                }

                return Json(new { success = false, message = "Failed to add to waitlist." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AddToWaitlist Error: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while adding to waitlist" });
            }
        }

        [Authorize]
        [HttpGet]
        public IActionResult ItineraryPdf(int bookingId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized();
            }

            var booking = _bookingRepo.GetByIdForUser(bookingId, userId);
            if (booking == null)
            {
                return NotFound();
            }

            var trip = booking.Trip ?? _tripRepo.GetById(booking.TripID);
            if (trip == null)
            {
                return NotFound();
            }

            var dates = ResolveBookingDates(booking);
            var userName = booking.User != null
                ? $"{booking.User.first_name} {booking.User.last_name}"
                : (User.Identity?.Name ?? "Traveler");
            var userEmail = booking.User?.email ?? User.Identity?.Name ?? "";
            var bookingRef = $"BK-{booking.BookingID:00000}";

            var lines = new List<string>
            {
                "--- TRAVELER ---",
                $"Name: {userName}",
                $"Email: {userEmail}",
                $"Booking Ref: {bookingRef}",
                " ",
                "--- TRIP DETAILS ---",
                $"Destination: {trip.Destination}",
                $"Country: {trip.Country}",
                $"Package: {trip.PackageType}",
                $"Dates: {dates.Start:MMM dd, yyyy} - {dates.End:MMM dd, yyyy}",
                $"Duration: {(dates.End - dates.Start).Days} days",
                $"Status: {booking.Status}",
                " ",
                "--- TRAVEL PARTY ---",
                $"Travelers: {booking.Quantity}",
                " ",
                "--- PRICING ---",
                $"Unit Price: ${booking.UnitPrice:N2}",
                $"Total Paid: ${booking.TotalPrice:N2}",
                $"Booked On: {booking.BookingDate:MMM dd, yyyy}",
                " ",
                "--- NOTES ---",
                $"{trip.Description}"
            };

            var pdfBytes = BuildSimplePdf($"Itinerary - {trip.Destination}", lines);
            var safeName = trip.Destination.Replace(' ', '_');
            return File(pdfBytes, "application/pdf", $"Itinerary_{safeName}_{booking.BookingID}.pdf");
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CancelBooking([FromBody] CancelBookingRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Json(new { success = false, message = "User not authenticated" });
            }

            try
            {
                // Verify booking belongs to user and can be cancelled
                var booking = _bookingRepo.GetByIdForUser(request.BookingId, userId);
                if (booking == null)
                {
                    return Json(new { success = false, message = "Booking not found or access denied" });
                }

                if (booking.Status == "Cancelled")
                {
                    return Json(new { success = false, message = "This booking is already cancelled" });
                }

                // Check if cancellation is still allowed
                if (!_bookingRepo.CanCancelBooking(request.BookingId, userId, DateTime.UtcNow))
                {
                    return Json(new { success = false, message = "Cancellation deadline has passed" });
                }

                // Update booking status to Cancelled
                booking.Status = "Cancelled";
                _bookingRepo.Update(booking);

                // Restore available rooms
                var trip = _tripRepo.GetById(request.TripId);
                if (trip != null)
                {
                    trip.AvailableRooms += booking.Quantity;
                    _tripRepo.Update(trip);

                    // Process waitlist for freed rooms
                    await ProcessWaitlistForTrip(request.TripId, booking.Quantity);
                }

                return Json(new { success = true, message = "Booking cancelled successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
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

        private (DateTime Start, DateTime End) ResolveBookingDates(Booking booking)
        {
            if (booking.Trip == null)
            {
                return (DateTime.MinValue, DateTime.MinValue);
            }

            if (booking.SelectedDateIndex < 0)
            {
                return (booking.Trip.StartDate, booking.Trip.EndDate);
            }

            var variations = _dateRepo.GetByTripId(booking.TripID).ToList();
            if (booking.SelectedDateIndex >= 0 && booking.SelectedDateIndex < variations.Count)
            {
                var v = variations[booking.SelectedDateIndex];
                return (v.StartDate, v.EndDate);
            }

            return (booking.Trip.StartDate, booking.Trip.EndDate);
        }

        private static byte[] BuildSimplePdf(string title, IEnumerable<string> lines)
        {
            var contentBuilder = new StringBuilder();
            contentBuilder.AppendLine("BT");

            // Header block with simple logo and company name
            contentBuilder.AppendLine("/F1 20 Tf");
            contentBuilder.AppendLine("50 780 Td");
            contentBuilder.AppendLine($"({EscapePdfText("[TM] TravelMate")}) Tj");
            contentBuilder.AppendLine("0 -18 Td");
            contentBuilder.AppendLine("/F1 12 Tf");
            contentBuilder.AppendLine($"({EscapePdfText("Itinerary & Invoice")}) Tj");
            contentBuilder.AppendLine("0 -12 Td");
            contentBuilder.AppendLine($"({EscapePdfText("___________________________________________")}) Tj");

            // Title block
            contentBuilder.AppendLine("0 -26 Td");
            contentBuilder.AppendLine("/F1 16 Tf");
            contentBuilder.AppendLine($"({EscapePdfText(title)}) Tj");
            contentBuilder.AppendLine("0 -22 Td");
            contentBuilder.AppendLine("/F1 12 Tf");

            foreach (var line in lines)
            {
                // Section headers
                if (line.StartsWith("---"))
                {
                    contentBuilder.AppendLine("0 -6 Td");
                    contentBuilder.AppendLine($"({EscapePdfText(line)}) Tj");
                    contentBuilder.AppendLine("0 -14 Td");
                }
                else
                {
                    // Indent body lines for structure (ASCII dash to avoid unsupported glyphs)
                    contentBuilder.AppendLine($"({EscapePdfText("  - " + line)}) Tj");
                    contentBuilder.AppendLine("0 -14 Td");
                }
            }

            contentBuilder.AppendLine("ET");
            var contentString = contentBuilder.ToString();
            var contentBytes = Encoding.ASCII.GetBytes(contentString);

            var objects = new List<string>
            {
                "1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj",
                "2 0 obj << /Type /Pages /Count 1 /Kids [3 0 R] >> endobj",
                "3 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >> endobj",
                $"4 0 obj << /Length {contentBytes.Length} >> stream\n{contentString}\nendstream endobj",
                "5 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> endobj"
            };

            var pdfBuilder = new StringBuilder();
            pdfBuilder.AppendLine("%PDF-1.4");

            var offsets = new List<int>();
            foreach (var obj in objects)
            {
                offsets.Add(Encoding.ASCII.GetByteCount(pdfBuilder.ToString()));
                pdfBuilder.AppendLine(obj);
            }

            var xrefStart = Encoding.ASCII.GetByteCount(pdfBuilder.ToString());
            pdfBuilder.AppendLine("xref");
            pdfBuilder.AppendLine($"0 {objects.Count + 1}");
            pdfBuilder.AppendLine("0000000000 65535 f ");
            foreach (var offset in offsets)
            {
                pdfBuilder.AppendLine($"{offset:0000000000} 00000 n ");
            }

            pdfBuilder.AppendLine("trailer << /Size " + (objects.Count + 1) + " /Root 1 0 R >>");
            pdfBuilder.AppendLine("startxref");
            pdfBuilder.AppendLine(xrefStart.ToString());
            pdfBuilder.AppendLine("%%EOF");

            return Encoding.ASCII.GetBytes(pdfBuilder.ToString());
        }

        private static string EscapePdfText(string text)
        {
            return text.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
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

    // Request model for CancelBooking
    public class CancelBookingRequest
    {
        public int BookingId { get; set; }
        public int TripId { get; set; }
    }

    // Request model for AddToWaitlist
    public class WaitlistRequest
    {
        public int TripId { get; set; }
    }
}
