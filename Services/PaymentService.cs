using Microsoft.Extensions.Configuration;
using MVC_project.Data;
using MVC_project.Models;

namespace MVC_project.Services
{
    public class PaymentService
    {
        private readonly IConfiguration _config;
        private readonly TripRepository _tripRepo;

        public PaymentService(IConfiguration config, TripRepository tripRepo)
        {
            _config = config;
            _tripRepo = tripRepo;
        }

        public record CheckoutInfo(
            int TripId,
            string Destination,
            string Country,
            decimal UnitPrice,
            int Quantity,
            decimal Total,
            int AvailableRooms,
            DateTime StartDate,
            DateTime EndDate,
            string PackageType,
            int? AgeLimit,
            DateTime? DiscountEndDate,
            decimal Price,
            decimal? DiscountPrice);

        public CheckoutInfo? GetCheckoutInfo(int tripId, int quantity)
        {
            var trip = _tripRepo.GetById(tripId);
            if (trip == null) return null;
            var unit = trip.DiscountPrice.HasValue && trip.DiscountPrice < trip.Price ? trip.DiscountPrice.Value : trip.Price;
            var qty = quantity <= 0 ? 1 : quantity;
            return new CheckoutInfo(
                trip.TripID,
                trip.Destination,
                trip.Country,
                unit,
                qty,
                unit * qty,
                trip.AvailableRooms,
                trip.StartDate,
                trip.EndDate,
                trip.PackageType,
                trip.AgeLimit,
                trip.DiscountEndDate,
                trip.Price,
                trip.DiscountPrice);
        }

        // Simulated card charge: always succeeds for placeholder implementation
        public bool SimulateCardCharge(string userEmail, Trip trip, int quantity)
        {
            // In a real gateway integration, call provider to authorize/capture.
            // Here we just return success without storing any card data.
            return true;
        }

        // Placeholder PayPal methods
        public string CreatePayPalOrder(decimal amount, string currency)
        {
            // Would call PayPal Orders API; return a fake orderId for now
            return $"SIMULATED-PAYPAL-ORDER-{Guid.NewGuid().ToString("N").Substring(0,8)}";
        }

        public bool CapturePayPalOrder(string orderId)
        {
            // Would capture PayPal order; always succeed in placeholder
            return !string.IsNullOrWhiteSpace(orderId);
        }
    }
}
