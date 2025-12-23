using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MVC_project.Data;
using MVC_project.Services;
using System.Security.Claims;

namespace MVC_project.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        private readonly TripRepository _tripRepo;
        private readonly UserTripRepository _userTripRepo;
        private readonly PaymentService _paymentService;

        public BookingController(TripRepository tripRepo, UserTripRepository userTripRepo, PaymentService paymentService)
        {
            _tripRepo = tripRepo;
            _userTripRepo = userTripRepo;
            _paymentService = paymentService;
        }

        // GET: /Booking/CheckoutInfo?tripId=1&quantity=2
        [HttpGet]
        public IActionResult CheckoutInfo(int tripId, int quantity = 1)
        {
            var info = _paymentService.GetCheckoutInfo(tripId, quantity);
            if (info == null)
                return Json(new { success = false, message = "Trip not found" });

            return Json(new { success = true, checkout = info });
        }

        // GET: /Booking/CartCheckoutInfo
        [HttpGet]
        [Authorize]
        public IActionResult CartCheckoutInfo()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                return Json(new { success = false, message = "User not authenticated" });

            var cartItems = _userTripRepo.GetByUserId(userId).ToList();
            if (!cartItems.Any())
                return Json(new { success = false, message = "Your cart is empty." });

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

            return Json(new { success = true, checkout = new { itemCount = cartItems.Count, total = total, packages = packages } });
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
        public IActionResult BuyNow([FromBody] BuyNowRequest request)
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

            var qty = request.Quantity <= 0 ? 1 : request.Quantity;
            if (qty > trip.AvailableRooms)
            {
                return Json(new { success = false, message = $"Only {trip.AvailableRooms} rooms available for {trip.Destination}." });
            }

            // Legacy direct purchase (kept for backward compatibility)
            _userTripRepo.Add(userId, request.TripId, qty);
            _userTripRepo.Remove(userId, request.TripId);
            return Json(new { success = true, message = $"Purchase completed for {trip.Destination} (x{qty})." });
        }

        // POST: /Booking/PayCard
        [HttpPost]
        public IActionResult PayCard([FromBody] BuyNowRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                return Json(new { success = false, message = "User not authenticated" });

            var trip = _tripRepo.GetById(request.TripId);
            if (trip == null)
                return Json(new { success = false, message = "Trip not found" });

            var qty = request.Quantity <= 0 ? 1 : request.Quantity;
            if (qty > trip.AvailableRooms)
                return Json(new { success = false, message = $"Only {trip.AvailableRooms} rooms available for {trip.Destination}." });

            // Simulate card payment success
            var ok = _paymentService.SimulateCardCharge(userId, trip, qty);
            if (!ok)
                return Json(new { success = false, message = "Payment failed. Please try another method." });

            // On success: decrement availability and clear from cart
            trip.AvailableRooms -= qty;
            _tripRepo.Update(trip);
            _userTripRepo.Remove(userId, request.TripId);

            return Json(new { success = true, message = $"Payment successful. {qty} room(s) for {trip.Destination} booked!" });
        }

        // POST: /Booking/PayPalSimulate
        [HttpPost]
        public IActionResult PayPalSimulate([FromBody] BuyNowRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                return Json(new { success = false, message = "User not authenticated" });

            var trip = _tripRepo.GetById(request.TripId);
            if (trip == null)
                return Json(new { success = false, message = "Trip not found" });

            var qty = request.Quantity <= 0 ? 1 : request.Quantity;
            if (qty > trip.AvailableRooms)
                return Json(new { success = false, message = $"Only {trip.AvailableRooms} rooms available for {trip.Destination}." });

            // Create and capture a simulated PayPal order
            var info = _paymentService.GetCheckoutInfo(request.TripId, qty);
            if (info == null)
                return Json(new { success = false, message = "Trip not found" });

            var currency = HttpContext.Request.Headers["X-Currency"].FirstOrDefault() ?? "USD";
            var orderId = _paymentService.CreatePayPalOrder(info.Total, currency);
            var captured = _paymentService.CapturePayPalOrder(orderId);
            if (!captured)
                return Json(new { success = false, message = "PayPal capture failed." });

            // On success: decrement availability and clear from cart
            trip.AvailableRooms -= qty;
            _tripRepo.Update(trip);
            _userTripRepo.Remove(userId, request.TripId);

            return Json(new { success = true, message = $"PayPal payment successful. {qty} room(s) for {trip.Destination} booked!" });
        }

        // POST: /Booking/PayPalCartSimulate
        [HttpPost]
        public IActionResult PayPalCartSimulate()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                return Json(new { success = false, message = "User not authenticated" });

            var cartItems = _userTripRepo.GetByUserId(userId).ToList();
            if (!cartItems.Any())
                return Json(new { success = false, message = "Your cart is empty." });

            decimal grandTotal = 0m;
            foreach (var item in cartItems)
            {
                var trip = item.Trip ?? _tripRepo.GetById(item.TripID);
                if (trip == null)
                    return Json(new { success = false, message = $"Trip with ID {item.TripID} not found." });

                var qty = item.Quantity <= 0 ? 1 : item.Quantity;
                if (qty > trip.AvailableRooms)
                    return Json(new { success = false, message = $"Only {trip.AvailableRooms} rooms available for {trip.Destination}." });

                var unit = trip.DiscountPrice.HasValue && trip.DiscountPrice < trip.Price
                    ? trip.DiscountPrice.Value
                    : trip.Price;
                grandTotal += unit * qty;
            }

            var currency = HttpContext.Request.Headers["X-Currency"].FirstOrDefault() ?? "USD";
            var orderId = _paymentService.CreatePayPalOrder(grandTotal, currency);
            var captured = _paymentService.CapturePayPalOrder(orderId);
            if (!captured)
                return Json(new { success = false, message = "PayPal capture failed." });

            foreach (var item in cartItems)
            {
                var trip = item.Trip ?? _tripRepo.GetById(item.TripID);
                if (trip == null) continue;

                var qty = item.Quantity <= 0 ? 1 : item.Quantity;
                trip.AvailableRooms -= qty;
                if (trip.AvailableRooms < 0) trip.AvailableRooms = 0;
                _tripRepo.Update(trip);
            }

            _userTripRepo.RemoveAll(userId);
            return Json(new { success = true, message = $"PayPal payment successful. {cartItems.Count} trip(s) booked!" });
        }

        // POST: /Booking/PayPalDateSimulate
        [HttpPost]
        [Authorize]
        public IActionResult PayPalDateSimulate([FromBody] PayPalDateRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                return Json(new { success = false, message = "User not authenticated" });

            var userTrip = _userTripRepo.GetByUserId(userId).FirstOrDefault(ut => ut.UserTripID == request.UserTripId);
            if (userTrip == null)
                return Json(new { success = false, message = "Date not found in cart" });

            var trip = userTrip.Trip ?? _tripRepo.GetById(userTrip.TripID);
            if (trip == null)
                return Json(new { success = false, message = "Trip not found" });

            var qty = userTrip.Quantity <= 0 ? 1 : userTrip.Quantity;
            if (qty > trip.AvailableRooms)
                return Json(new { success = false, message = $"Only {trip.AvailableRooms} rooms available" });

            var unitPrice = trip.DiscountPrice.HasValue && trip.DiscountPrice < trip.Price
                ? trip.DiscountPrice.Value
                : trip.Price;
            var total = unitPrice * qty;

            var currency = HttpContext.Request.Headers["X-Currency"].FirstOrDefault() ?? "USD";
            var orderId = _paymentService.CreatePayPalOrder(total, currency);
            var captured = _paymentService.CapturePayPalOrder(orderId);
            if (!captured)
                return Json(new { success = false, message = "PayPal capture failed." });

            trip.AvailableRooms -= qty;
            if (trip.AvailableRooms < 0) trip.AvailableRooms = 0;
            _tripRepo.Update(trip);

            _userTripRepo.RemoveByUserTripId(request.UserTripId);
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