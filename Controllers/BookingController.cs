using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MVC_project.Data;
using MVC_project.Models;
using MVC_project.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using MVC_project.Hubs;
using MVC_project.Data;

namespace MVC_project.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        private readonly TripRepository _tripRepo;
        private readonly UserTripRepository _userTripRepo;
        private readonly BookingRepository _bookingRepo;
        private readonly PaymentService _paymentService;
        private readonly WaitlistRepository _waitlistRepo;
        private readonly UserRepository _userRepo;
        private readonly EmailService _emailService;
        private readonly IHubContext<TripHub> _tripHub;
        private readonly TripDateRepository _tripDateRepo;

        public BookingController(
            TripRepository tripRepo, 
            UserTripRepository userTripRepo,
            BookingRepository bookingRepo,
            PaymentService paymentService,
            WaitlistRepository waitlistRepo,
            UserRepository userRepo,
            EmailService emailService,
            IHubContext<TripHub> tripHub,
            TripDateRepository tripDateRepo)
        {
            _tripRepo = tripRepo;
            _userTripRepo = userTripRepo;
            _bookingRepo = bookingRepo;
            _paymentService = paymentService;
            _waitlistRepo = waitlistRepo;
            _userRepo = userRepo;
            _emailService = emailService;
            _tripHub = tripHub;
            _tripDateRepo = tripDateRepo;
        }

        // GET: /Booking/CheckoutInfo?tripId=1&quantity=2
        [HttpGet]
        public IActionResult CheckoutInfo(int tripId, int quantity = 1)
        {
            var trip = _tripRepo.GetById(tripId);
            if (trip == null)
                return Json(new { success = false, message = "Trip not found" });

            if (IsBookingClosed(trip))
            {
                RemoveTripFromAllCarts(trip);
                return Json(new { success = false, message = BookingClosedMessage(trip) });
            }

            var info = _paymentService.GetCheckoutInfo(tripId, quantity);
            return info == null
                ? Json(new { success = false, message = "Trip not found" })
                : Json(new { success = true, checkout = info });
        }

        // GET: /Booking/CartCheckoutInfo
        [HttpGet]
        [Authorize]
        public IActionResult CartCheckoutInfo()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                return Json(new { success = false, message = "User not authenticated" });

            var todayDate = DateTime.UtcNow.Date;
            var removedClosed = _userTripRepo.RemoveClosedByUserId(userId, todayDate);

            var cartItems = _userTripRepo.GetByUserId(userId).ToList();
            if (!cartItems.Any())
            {
                var emptyMessage = removedClosed > 0
                    ? "Trips whose booking window closed were removed from your cart."
                    : "Your cart is empty.";
                return Json(new { success = false, message = emptyMessage });
            }

            decimal total = 0m;
            var packages = new List<object>();
            
            foreach (var item in cartItems)
            {
                var trip = item.Trip ?? _tripRepo.GetById(item.TripID);
                if (trip == null) continue;

                var qty = item.Quantity <= 0 ? 1 : item.Quantity;
                var unit = trip.DiscountPrice.HasValue && trip.DiscountPrice < trip.Price
                    ? trip.DiscountPrice.Value
                    : trip.Price;
                var itemTotal = unit * qty;
                total += itemTotal;
                
                packages.Add(new
                {
                    destination = trip.Destination,
                    country = trip.Country,
                    packageType = trip.PackageType,
                    quantity = qty,
                    unitPrice = unit,
                    originalPrice = trip.Price,
                    discountPrice = trip.DiscountPrice,
                    itemTotal = itemTotal,
                    startDate = trip.StartDate,
                    endDate = trip.EndDate,
                    ageLimit = trip.AgeLimit
                });
            }

            return Json(new { success = true, checkout = new { itemCount = cartItems.Count, total = total, packages = packages }, removedClosed });
        }

        // GET: /Booking/DateCheckoutInfo?userTripId=123
        [HttpGet]
        [Authorize]
        public IActionResult DateCheckoutInfo(int userTripId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                return Json(new { success = false, message = "User not authenticated" });

            var userTrip = _userTripRepo.GetByUserId(userId).FirstOrDefault(ut => ut.UserTripID == userTripId);
            if (userTrip == null)
                return Json(new { success = false, message = "Date not found in cart" });

            var trip = userTrip.Trip ?? _tripRepo.GetById(userTrip.TripID);
            if (trip == null)
                return Json(new { success = false, message = "Trip not found" });

            if (IsBookingClosed(trip))
            {
                RemoveTripFromAllCarts(trip);
                return Json(new { success = false, message = BookingClosedMessage(trip) });
            }

            var qty = userTrip.Quantity <= 0 ? 1 : userTrip.Quantity;
            var unitPrice = trip.DiscountPrice.HasValue && trip.DiscountPrice < trip.Price
                ? trip.DiscountPrice.Value
                : trip.Price;
            var total = unitPrice * qty;

            var checkoutInfo = new
            {
                userTripId = userTripId,
                destination = trip.Destination,
                country = trip.Country,
                packageType = trip.PackageType,
                quantity = qty,
                unitPrice = unitPrice,
                total = total,
                availableRooms = trip.AvailableRooms,
                ageLimit = trip.AgeLimit,
                discountPrice = trip.DiscountPrice,
                discountEndDate = trip.DiscountEndDate,
                startDate = trip.StartDate,
                endDate = trip.EndDate
            };

            return Json(new { success = true, checkout = checkoutInfo });
        }

        // POST: /Booking/BuyNow
        [HttpPost]
        public async Task<IActionResult> BuyNow([FromBody] BuyNowRequest request)
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

            if (IsBookingClosed(trip))
            {
                RemoveTripFromAllCarts(trip);
                return Json(new { success = false, message = BookingClosedMessage(trip) });
            }

            if (IsBookingClosed(trip))
            {
                RemoveTripFromAllCarts(trip);
                return Json(new { success = false, message = BookingClosedMessage(trip) });
            }

            var qty = request.Quantity <= 0 ? 1 : request.Quantity;
            // Atomic reservation to prevent oversell (main date only)
            var reserved = _tripRepo.TryReserveRooms(trip.TripID, qty);
            if (!reserved)
            {
                var fresh = _tripRepo.GetById(trip.TripID);
                var left = fresh?.AvailableRooms ?? 0;
                return Json(new { success = false, message = $"Only {left} rooms available for {trip.Destination}." });
            }
            // Notify clients about availability change
            var updated = _tripRepo.GetById(trip.TripID);
            await _tripHub.Clients.All.SendAsync("availabilityUpdated", new { tripId = trip.TripID, availableRooms = updated?.AvailableRooms ?? 0, selectedDateIndex = -1 });
            
            return Json(new { success = true, message = $"Purchase completed for {trip.Destination} (x{qty})." });
        }

        // POST: /Booking/PayCard
        [HttpPost]
        public async Task<IActionResult> PayCard([FromBody] BuyNowRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                return Json(new { success = false, message = "User not authenticated" });

            if (GetUpcomingBookingsCount(userId) >= 3)
                return Json(new { success = false, message = "Limit reached: you can have at most 3 upcoming bookings." });

            var trip = _tripRepo.GetById(request.TripId);
            if (trip == null)
                return Json(new { success = false, message = "Trip not found" });

            if (IsBookingClosed(trip))
            {
                RemoveTripFromAllCarts(trip);
                return Json(new { success = false, message = BookingClosedMessage(trip) });
            }

            if (IsBookingClosed(trip))
            {
                RemoveTripFromAllCarts(trip);
                return Json(new { success = false, message = BookingClosedMessage(trip) });
            }

            var qty = request.Quantity <= 0 ? 1 : request.Quantity;

            // Simulate card payment success
            var ok = _paymentService.SimulateCardCharge(userId, trip, qty);
            if (!ok)
                return Json(new { success = false, message = "Payment failed. Please try another method." });

            var selectedDateIndex = -1; // PayCard currently uses main date

            // Reserve inventory atomically (post-payment)
            var reserved = _tripRepo.TryReserveRooms(trip.TripID, qty);
            if (!reserved)
            {
                var fresh = _tripRepo.GetById(trip.TripID);
                var left = fresh?.AvailableRooms ?? 0;
                return Json(new { success = false, message = $"Payment captured, but inventory changed. Only {left} rooms remain. Please contact support for a refund." });
            }

            // Record booking
            AddBookingRecord(userId, trip, qty, selectedDateIndex);

            // Notify clients about availability change
            var updated = _tripRepo.GetById(trip.TripID);
            await _tripHub.Clients.All.SendAsync("availabilityUpdated", new { tripId = trip.TripID, availableRooms = updated?.AvailableRooms ?? 0, selectedDateIndex });

            // On success: clear from cart
            _userTripRepo.Remove(userId, request.TripId);

            // Mark waitlist entry as booked if user was from waitlist
            _waitlistRepo.MarkAsBooked(userId, request.TripId);

            // Send confirmation email
            var user = _userRepo.GetById(userId);
            if (user != null)
            {
                await _emailService.SendPaymentConfirmationAsync(
                    user.email,
                    user.first_name,
                    trip.Destination,
                    _paymentService.GetCheckoutInfo(request.TripId, qty)?.Total ?? (trip.Price * qty),
                    qty,
                    trip.StartDate,
                    trip.EndDate,
                    trip.PackageType
                );
            }

            return Json(new { success = true, message = $"Payment successful. {qty} room(s) for {trip.Destination} booked!" });
        }

        // POST: /Booking/PayPalSimulate
        [HttpPost]
        public async Task<IActionResult> PayPalSimulate([FromBody] BuyNowRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                return Json(new { success = false, message = "User not authenticated" });

            if (GetUpcomingBookingsCount(userId) >= 3)
                return Json(new { success = false, message = "Limit reached: you can have at most 3 upcoming bookings." });

            var trip = _tripRepo.GetById(request.TripId);
            if (trip == null)
                return Json(new { success = false, message = "Trip not found" });

            if (IsBookingClosed(trip))
            {
                RemoveTripFromAllCarts(trip);
                return Json(new { success = false, message = BookingClosedMessage(trip) });
            }

            var qty = request.Quantity <= 0 ? 1 : request.Quantity;

            // Create and capture a simulated PayPal order
            var info = _paymentService.GetCheckoutInfo(request.TripId, qty);
            if (info == null)
                return Json(new { success = false, message = "Trip not found" });

            var currency = HttpContext.Request.Headers["X-Currency"].FirstOrDefault() ?? "USD";
            var orderId = _paymentService.CreatePayPalOrder(info.Total, currency);
            var captured = _paymentService.CapturePayPalOrder(orderId);
            if (!captured)
                return Json(new { success = false, message = "PayPal capture failed." });

            var selectedDateIndex = -1; // PayPalSimulate currently uses main date

            // Reserve inventory atomically (post-payment)
            var reserved = _tripRepo.TryReserveRooms(trip.TripID, qty);
            if (!reserved)
            {
                var fresh = _tripRepo.GetById(trip.TripID);
                var left = fresh?.AvailableRooms ?? 0;
                return Json(new { success = false, message = $"PayPal captured, but inventory changed. Only {left} rooms remain. Please contact support for a refund." });
            }

            // Record booking
            AddBookingRecord(userId, trip, qty, selectedDateIndex);

            // Notify clients about availability change
            var updated = _tripRepo.GetById(trip.TripID);
            await _tripHub.Clients.All.SendAsync("availabilityUpdated", new { tripId = trip.TripID, availableRooms = updated?.AvailableRooms ?? 0, selectedDateIndex });

            // Mark waitlist entry as booked if user was from waitlist
            _waitlistRepo.MarkAsBooked(userId, request.TripId);

            // On success: clear from cart
            _userTripRepo.Remove(userId, request.TripId);

            // Send confirmation email
            var user = _userRepo.GetById(userId);
            if (user != null)
            {
                await _emailService.SendPaymentConfirmationAsync(
                    user.email,
                    user.first_name,
                    trip.Destination,
                    info.Total,
                    qty,
                    trip.StartDate,
                    trip.EndDate,
                    trip.PackageType
                );
            }

            return Json(new { success = true, message = $"PayPal payment successful. {qty} room(s) for {trip.Destination} booked!" });
        }

        // POST: /Booking/PayPalCartSimulate
        [HttpPost]
        public async Task<IActionResult> PayPalCartSimulate()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                return Json(new { success = false, message = "User not authenticated" });

            var todayDate = DateTime.UtcNow.Date;
            var removedClosed = _userTripRepo.RemoveClosedByUserId(userId, todayDate);

            var cartItems = _userTripRepo.GetByUserId(userId).ToList();
            if (!cartItems.Any())
            {
                var emptyMessage = removedClosed > 0
                    ? "Trips whose booking window closed were removed from your cart."
                    : "Your cart is empty.";
                return Json(new { success = false, message = emptyMessage });
            }

            var existingUpcoming = GetUpcomingBookingsCount(userId);
            if (existingUpcoming + cartItems.Count > 3)
                return Json(new { success = false, message = $"Limit reached: you already have {existingUpcoming} upcoming booking(s). You can book {Math.Max(0, 3 - existingUpcoming)} more." });

            decimal grandTotal = 0m;
            var cartDetails = new List<(string destination, int quantity, decimal total, DateTime startDate, DateTime endDate, string packageType)>();
            
            foreach (var item in cartItems)
            {
                var trip = item.Trip ?? _tripRepo.GetById(item.TripID);
                if (trip == null)
                    return Json(new { success = false, message = $"Trip with ID {item.TripID} not found." });

                if (IsBookingClosed(trip))
                {
                    RemoveTripFromAllCarts(trip);
                    return Json(new { success = false, message = BookingClosedMessage(trip) });
                }

                var qty = item.Quantity <= 0 ? 1 : item.Quantity;
                if (qty > trip.AvailableRooms)
                    return Json(new { success = false, message = $"Only {trip.AvailableRooms} rooms available for {trip.Destination}." });

                var unit = trip.DiscountPrice.HasValue && trip.DiscountPrice < trip.Price
                    ? trip.DiscountPrice.Value
                    : trip.Price;
                var itemTotal = unit * qty;
                grandTotal += itemTotal;
                cartDetails.Add((trip.Destination, qty, itemTotal, trip.StartDate, trip.EndDate, trip.PackageType));
            }

            var currency = HttpContext.Request.Headers["X-Currency"].FirstOrDefault() ?? "USD";
            var orderId = _paymentService.CreatePayPalOrder(grandTotal, currency);
            var captured = _paymentService.CapturePayPalOrder(orderId);
            if (!captured)
                return Json(new { success = false, message = "PayPal capture failed." });

            // Record bookings for each cart item and decrease availability
            foreach (var item in cartItems)
            {
                var trip = item.Trip ?? _tripRepo.GetById(item.TripID);
                if (trip == null) continue;
                var qty = item.Quantity <= 0 ? 1 : item.Quantity;

                var selectedDateIndex = item.SelectedDateIndex;
                bool reserved;
                int newAvailable;

                if (selectedDateIndex >= 0)
                {
                    var tripDates = _tripDateRepo.GetByTripId(trip.TripID).ToList();
                    if (selectedDateIndex >= tripDates.Count)
                    {
                        return Json(new { success = false, message = "Selected date is no longer available." });
                    }

                    var tripDate = tripDates[selectedDateIndex];
                    reserved = _tripDateRepo.TryReserveRooms(tripDate.TripDateID, qty);
                    if (!reserved)
                    {
                        var fresh = _tripDateRepo.GetById(tripDate.TripDateID);
                        var left = fresh?.AvailableRooms ?? 0;
                        return Json(new { success = false, message = $"Inventory changed during checkout for {trip.Destination} (date option). Only {left} rooms remain." });
                    }
                    newAvailable = _tripDateRepo.GetById(tripDate.TripDateID)?.AvailableRooms ?? 0;
                }
                else
                {
                    reserved = _tripRepo.TryReserveRooms(trip.TripID, qty);
                    if (!reserved)
                    {
                        var fresh = _tripRepo.GetById(trip.TripID);
                        var left = fresh?.AvailableRooms ?? 0;
                        return Json(new { success = false, message = $"Inventory changed during checkout for {trip.Destination}. Only {left} rooms remain." });
                    }
                    newAvailable = _tripRepo.GetById(trip.TripID)?.AvailableRooms ?? 0;
                }

                AddBookingRecord(userId, trip, qty, item.SelectedDateIndex);

                // Notify clients about availability change (include selectedDateIndex for variations)
                await _tripHub.Clients.All.SendAsync("availabilityUpdated", new { tripId = trip.TripID, availableRooms = newAvailable, selectedDateIndex });
            }

            // Mark all waitlist entries as booked for items in cart
            foreach (var item in cartItems)
            {
                _waitlistRepo.MarkAsBooked(userId, item.TripID);
            }

            // On success: clear cart
            _userTripRepo.RemoveAll(userId);

            // Send confirmation emails for each trip in cart
            var user = _userRepo.GetById(userId);
            if (user != null)
            {
                foreach (var (destination, quantity, total, startDate, endDate, packageType) in cartDetails)
                {
                    await _emailService.SendPaymentConfirmationAsync(
                        user.email,
                        user.first_name,
                        destination,
                        total,
                        quantity,
                        startDate,
                        endDate,
                        packageType
                    );
                }
            }

            return Json(new { success = true, message = $"PayPal payment successful. {cartItems.Count} trip(s) booked!" });
        }

        // POST: /Booking/PayPalDateSimulate
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> PayPalDateSimulate([FromBody] PayPalDateRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                return Json(new { success = false, message = "User not authenticated" });

            if (GetUpcomingBookingsCount(userId) >= 3)
                return Json(new { success = false, message = "Limit reached: you can have at most 3 upcoming bookings." });

            var userTrip = _userTripRepo.GetByUserId(userId).FirstOrDefault(ut => ut.UserTripID == request.UserTripId);
            if (userTrip == null)
                return Json(new { success = false, message = "Date not found in cart" });

            var trip = userTrip.Trip ?? _tripRepo.GetById(userTrip.TripID);
            if (trip == null)
                return Json(new { success = false, message = "Trip not found" });

            if (IsBookingClosed(trip))
            {
                RemoveTripFromAllCarts(trip);
                return Json(new { success = false, message = BookingClosedMessage(trip) });
            }

            var qty = userTrip.Quantity <= 0 ? 1 : userTrip.Quantity;

            var unitPrice = trip.DiscountPrice.HasValue && trip.DiscountPrice < trip.Price
                ? trip.DiscountPrice.Value
                : trip.Price;
            var total = unitPrice * qty;

            var currency = HttpContext.Request.Headers["X-Currency"].FirstOrDefault() ?? "USD";
            var orderId = _paymentService.CreatePayPalOrder(total, currency);
            var captured = _paymentService.CapturePayPalOrder(orderId);
            if (!captured)
                return Json(new { success = false, message = "PayPal capture failed." });

            var selectedDateIndex = userTrip.SelectedDateIndex;

            // Reserve inventory atomically (post-payment)
            bool reserved;
            int newAvailable;

            if (selectedDateIndex >= 0)
            {
                var tripDates = _tripDateRepo.GetByTripId(trip.TripID).ToList();
                if (selectedDateIndex >= tripDates.Count)
                    return Json(new { success = false, message = "Selected date is no longer available." });

                var tripDate = tripDates[selectedDateIndex];
                reserved = _tripDateRepo.TryReserveRooms(tripDate.TripDateID, qty);
                if (!reserved)
                {
                    var fresh = _tripDateRepo.GetById(tripDate.TripDateID);
                    var left = fresh?.AvailableRooms ?? 0;
                    return Json(new { success = false, message = $"PayPal captured, but inventory changed. Only {left} rooms remain for {trip.Destination}." });
                }
                newAvailable = _tripDateRepo.GetById(tripDate.TripDateID)?.AvailableRooms ?? 0;
            }
            else
            {
                reserved = _tripRepo.TryReserveRooms(trip.TripID, qty);
                if (!reserved)
                {
                    var fresh = _tripRepo.GetById(trip.TripID);
                    var left = fresh?.AvailableRooms ?? 0;
                    return Json(new { success = false, message = $"PayPal captured, but inventory changed. Only {left} rooms remain for {trip.Destination}." });
                }
                newAvailable = _tripRepo.GetById(trip.TripID)?.AvailableRooms ?? 0;
            }

            // Record booking for this specific date selection
            AddBookingRecord(userId, trip, qty, selectedDateIndex);

            // Notify clients about availability change
            await _tripHub.Clients.All.SendAsync("availabilityUpdated", new { tripId = trip.TripID, availableRooms = newAvailable, selectedDateIndex });

            // Mark waitlist entry as booked if user was from waitlist
            _waitlistRepo.MarkAsBooked(userId, userTrip.TripID);

            // On success: clear from cart
            _userTripRepo.RemoveByUserTripId(request.UserTripId);

            // Send confirmation email
            var user = _userRepo.GetById(userId);
            if (user != null)
            {
                await _emailService.SendPaymentConfirmationAsync(
                    user.email,
                    user.first_name,
                    trip.Destination,
                    total,
                    qty,
                    trip.StartDate,
                    trip.EndDate,
                    trip.PackageType
                );
            }

            return Json(new { success = true, message = $"Payment successful! Booked {trip.Destination} for {qty} person(s)." });
        }

        // POST: /Booking/Checkout
        [HttpPost]
        public IActionResult Checkout()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Json(new { success = false, message = "User not authenticated" });
            }

            var removed = _userTripRepo.RemoveAll(userId);
            if (removed == 0)
            {
                return Json(new { success = false, message = "Your cart is empty" });
            }

            return Json(new { success = true, message = "Checkout complete. Your bookings are confirmed!" });
        }

        // POST: /Booking/AddToCart - Add trip to cart or waitlist if no rooms
        [HttpPost]
        public IActionResult AddToCart([FromBody] BuyNowRequest request)
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

            if (IsBookingClosed(trip))
            {
                RemoveTripFromAllCarts(trip);
                return Json(new { success = false, message = BookingClosedMessage(trip) });
            }

            var qty = request.Quantity <= 0 ? 1 : request.Quantity;

            // Check if no rooms available
            if (trip.AvailableRooms == 0)
            {
                // Check if user is already on waitlist
                if (_waitlistRepo.IsUserOnWaitlist(userId, request.TripId))
                {
                    return Json(new 
                    { 
                        success = false, 
                        onWaitlist = true,
                        message = $"You are already on the waitlist for {trip.Destination}. We'll notify you when a spot opens up!" 
                    });
                }

                // Add to waitlist
                var added = _waitlistRepo.AddToWaitlist(userId, request.TripId);
                if (added)
                {
                    return Json(new 
                    { 
                        success = true, 
                        onWaitlist = true,
                        message = $"No rooms available for {trip.Destination}. You've been added to the waitlist and will be notified when a spot opens up!" 
                    });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to add to waitlist" });
                }
            }

            // Rooms are available - check if enough rooms for requested quantity
            if (qty > trip.AvailableRooms)
            {
                return Json(new 
                { 
                    success = false, 
                    message = $"Only {trip.AvailableRooms} room(s) available for {trip.Destination}." 
                });
            }

            // Check TOTAL quantity across all existing cart entries for this trip
            var existingTotal = _userTripRepo.GetTotalQuantityForTrip(userId, request.TripId);
            var projectedTotal = existingTotal + qty;

            // If adding would exceed availability, cap the quantity
            if (projectedTotal > trip.AvailableRooms)
            {
                var canAdd = trip.AvailableRooms - existingTotal;
                
                if (canAdd > 0)
                {
                    // Cap to remaining available
                    qty = canAdd;
                    _userTripRepo.Add(userId, request.TripId, qty);
                    return Json(new 
                    { 
                        success = true, 
                        capped = true,
                        message = $"{trip.Destination} (x{qty}) added to cart. {qty} slot(s) remain available." 
                    });
                }
                else
                {
                    // No slots available - add to waitlist
                    if (_waitlistRepo.IsUserOnWaitlist(userId, request.TripId))
                    {
                        return Json(new 
                        { 
                            success = false, 
                            onWaitlist = true,
                            message = $"You are already on the waitlist for {trip.Destination}." 
                        });
                    }

                    var added = _waitlistRepo.AddToWaitlist(userId, request.TripId);
                    if (added)
                    {
                        return Json(new 
                        { 
                            success = true, 
                            onWaitlist = true,
                            message = $"Cart is at capacity for {trip.Destination}. You've been added to the waitlist!" 
                        });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Failed to add to waitlist" });
                    }
                }
            }

            // Add to cart
            // NOTE: Inventory is NOT decremented here - it only changes after payment is completed
            _userTripRepo.Add(userId, request.TripId, qty);
            
            return Json(new 
            { 
                success = true, 
                message = $"{trip.Destination} (x{qty}) added to cart!" 
            });
        }

        private Booking AddBookingRecord(int userId, Trip trip, int quantity, int selectedDateIndex)
        {
            var unitPrice = trip.DiscountPrice.HasValue && trip.DiscountPrice < trip.Price
                ? trip.DiscountPrice.Value
                : trip.Price;

            var booking = new Booking
            {
                UserId = userId,
                TripID = trip.TripID,
                Quantity = quantity,
                UnitPrice = unitPrice,
                TotalPrice = unitPrice * quantity,
                BookingDate = DateTime.UtcNow,
                SelectedDateIndex = selectedDateIndex,
                Status = "Confirmed",
                Trip = trip
            };

            return _bookingRepo.Add(booking);
        }

        private int GetUpcomingBookingsCount(int userId)
        {
            var todayUtc = DateTime.UtcNow.Date;
            return _bookingRepo.CountUpcoming(userId, todayUtc);
        }

        private bool IsBookingClosed(Trip trip)
        {
            return trip.LatestBookingDate.HasValue && DateTime.UtcNow.Date > trip.LatestBookingDate.Value.Date;
        }

        private int RemoveTripFromAllCarts(Trip trip)
        {
            return _userTripRepo.RemoveAllByTripId(trip.TripID);
        }

        private string BookingClosedMessage(Trip trip)
        {
            var cutoff = trip.LatestBookingDate?.ToString("MMM dd, yyyy") ?? "the cutoff date";
            return $"Booking closed on {cutoff}. This trip remains visible for browsing only.";
        }
    }

    public class BuyNowRequest
    {
        public int TripId { get; set; }
        public int Quantity { get; set; }
    }
    public class PayPalDateRequest
    {
        public int UserTripId { get; set; }
    }
}