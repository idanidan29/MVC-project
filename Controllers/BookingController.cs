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

        // POST: /Booking/BuyNow
        [HttpPost]
        public IActionResult BuyNow([FromBody] BuyNowRequest request)
        {
            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userEmail))
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
            _userTripRepo.Add(userEmail, request.TripId, qty);
            _userTripRepo.Remove(userEmail, request.TripId);
            return Json(new { success = true, message = $"Purchase completed for {trip.Destination} (x{qty})." });
        }

        // POST: /Booking/PayCard
        [HttpPost]
        public IActionResult PayCard([FromBody] BuyNowRequest request)
        {
            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userEmail))
                return Json(new { success = false, message = "User not authenticated" });

            var trip = _tripRepo.GetById(request.TripId);
            if (trip == null)
                return Json(new { success = false, message = "Trip not found" });

            var qty = request.Quantity <= 0 ? 1 : request.Quantity;
            if (qty > trip.AvailableRooms)
                return Json(new { success = false, message = $"Only {trip.AvailableRooms} rooms available for {trip.Destination}." });

            // Simulate card payment success
            var ok = _paymentService.SimulateCardCharge(userEmail, trip, qty);
            if (!ok)
                return Json(new { success = false, message = "Payment failed. Please try another method." });

            // On success: decrement availability and clear from cart
            trip.AvailableRooms -= qty;
            _tripRepo.Update(trip);
            _userTripRepo.Remove(userEmail, request.TripId);

            return Json(new { success = true, message = $"Payment successful. {qty} room(s) for {trip.Destination} booked!" });
        }

        // POST: /Booking/PayPalSimulate
        [HttpPost]
        public IActionResult PayPalSimulate([FromBody] BuyNowRequest request)
        {
            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userEmail))
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
            _userTripRepo.Remove(userEmail, request.TripId);

            return Json(new { success = true, message = $"PayPal payment successful. {qty} room(s) for {trip.Destination} booked!" });
        }

        // POST: /Booking/Checkout
        [HttpPost]
        public IActionResult Checkout()
        {
            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                return Json(new { success = false, message = "User not authenticated" });
            }

            var removed = _userTripRepo.RemoveAll(userEmail);
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
}