using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MVC_project.Data;
using System.Security.Claims;

namespace MVC_project.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        private readonly TripRepository _tripRepo;
        private readonly UserTripRepository _userTripRepo;

        public BookingController(TripRepository tripRepo, UserTripRepository userTripRepo)
        {
            _tripRepo = tripRepo;
            _userTripRepo = userTripRepo;
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

            // Ensure in cart for consistency (optional)
            _userTripRepo.Add(userEmail, request.TripId, qty);

            // Simulate payment success by removing from cart
            _userTripRepo.Remove(userEmail, request.TripId);

            return Json(new { success = true, message = $"Payment completed for {trip.Destination} (x{qty}). Enjoy your trip!" });
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