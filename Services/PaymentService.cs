using Microsoft.Extensions.Configuration;
using MVC_project.Data;
using MVC_project.Models;

namespace MVC_project.Services
{
    /// <summary>
    /// Service for payment processing and checkout operations.
    /// Handles both simulated card payments and PayPal integration.
    /// In production, this would integrate with real payment gateways (Stripe, PayPal, etc.).
    /// Currently uses placeholder methods for development/testing.
    /// </summary>
    public class PaymentService
    {
        private readonly IConfiguration _config;
        private readonly TripRepository _tripRepo;

        public PaymentService(IConfiguration config, TripRepository tripRepo)
        {
            _config = config;      // Access to appsettings for payment API keys
            _tripRepo = tripRepo;  // Access trip data for pricing/availability
        }

        /// <summary>
        /// Record type for checkout information.
        /// 
        /// Why use record?
        /// - Immutable by default (can't accidentally modify checkout data)
        /// - Value-based equality (two CheckoutInfo with same data are considered equal)
        /// - Concise syntax for data transfer objects
        /// 
        /// Contains all information needed for payment page display and processing.
        /// </summary>
        public record CheckoutInfo(
            int TripId,              // Trip being purchased
            string Destination,      // Display name
            string Country,          // Display location
            decimal UnitPrice,       // Price per person (after discount if applicable)
            int Quantity,            // Number of people/rooms
            decimal Total,           // UnitPrice * Quantity
            int AvailableRooms,      // Current availability
            DateTime StartDate,      // Trip dates
            DateTime EndDate,
            string PackageType,      // Package category (Adventure, Luxury, etc.)
            int? AgeLimit,           // Minimum age requirement (null if none)
            DateTime? DiscountEndDate,  // When discount expires (null if no discount)
            decimal Price,           // Original price (before discount)
            decimal? DiscountPrice);  // Discounted price (null if no discount)

        /// <summary>
        /// Retrieves and calculates checkout information for a trip.
        /// 
        /// Price calculation logic:
        /// 1. Check if trip has active discount (DiscountPrice exists)
        /// 2. Use discount price if available, otherwise use regular price
        /// 3. Multiply by quantity for total
        /// 
        /// Returns null if trip doesn't exist.
        /// Validates quantity (minimum 1).
        /// </summary>
        public CheckoutInfo? GetCheckoutInfo(int tripId, int quantity)
        {
            var trip = _tripRepo.GetById(tripId);  // Fetch trip from database
            if (trip == null) return null;  // Trip doesn't exist
            
            // Determine unit price: use discount if available and lower than regular price
            var unit = trip.DiscountPrice.HasValue && trip.DiscountPrice < trip.Price 
                ? trip.DiscountPrice.Value   // Use discounted price
                : trip.Price;                 // Use regular price
            
            var qty = quantity <= 0 ? 1 : quantity;  // Ensure minimum quantity of 1
            
            // Create immutable checkout info record
            return new CheckoutInfo(
                trip.TripID,
                trip.Destination,
                trip.Country,
                unit,                     // Price per person
                qty,                      // Number of people
                unit * qty,               // Total amount
                trip.AvailableRooms,
                trip.StartDate,
                trip.EndDate,
                trip.PackageType,
                trip.AgeLimit,
                trip.DiscountEndDate,
                trip.Price,               // Original price for display
                trip.DiscountPrice);      // Discount price for display
        }

        /// <summary>
        /// Simulates card payment processing (placeholder implementation).
        /// 
        /// In production, this would:
        /// 1. Tokenize card details (never store raw card numbers!)
        /// 2. Call payment gateway API (Stripe.com, Authorize.Net, etc.)
        /// 3. Handle 3D Secure authentication if required
        /// 4. Return transaction ID and authorization code
        /// 5. Handle declined cards, insufficient funds, etc.
        /// 
        /// Currently always returns true for development/testing.
        /// Real implementation would validate amount, check available balance, etc.
        /// </summary>
        public bool SimulateCardCharge(int userId, Trip trip, int quantity)
        {
            // In a real gateway integration, call provider to authorize/capture.
            // Example: var result = await _stripeClient.Charges.CreateAsync(chargeOptions);
            // Would return result.Status == "succeeded"
            
            // Here we just return success without storing any card data (PCI compliance)
            return true;  // Placeholder success
        }

        /// <summary>
        /// Creates a PayPal order for payment processing (placeholder implementation).
        /// 
        /// In production, this would:
        /// 1. Call PayPal Orders API to create order
        /// 2. Receive order ID and approval URL
        /// 3. Return order ID to frontend
        /// 4. Frontend redirects user to PayPal approval URL
        /// 5. User approves payment on PayPal site
        /// 6. PayPal redirects back with order ID
        /// 7. Backend captures the order (see CapturePayPalOrder)
        /// 
        /// Returns simulated order ID for testing.
        /// </summary>
        public string CreatePayPalOrder(decimal amount, string currency)
        {
            // Would call PayPal Orders API
            // Example: var order = await _payPalClient.Orders.CreateAsync(orderRequest);
            // return order.Id;
            
            // Return fake orderId for testing
            return $"SIMULATED-PAYPAL-ORDER-{Guid.NewGuid().ToString("N").Substring(0,8)}";  // 8-char hex ID
        }

        /// <summary>
        /// Captures (completes) a PayPal order after user approval (placeholder).
        /// 
        /// In production, this would:
        /// 1. Call PayPal Orders API to capture payment
        /// 2. Funds are transferred from customer to merchant account
        /// 3. Receive transaction ID and status
        /// 4. Handle failed captures, insufficient funds, etc.
        /// 
        /// Returns true if capture successful, false otherwise.
        /// Currently returns true for any non-empty orderId (testing).
        /// </summary>
        public bool CapturePayPalOrder(string orderId)
        {
            // Would capture PayPal order
            // Example: var result = await _payPalClient.Orders.CaptureAsync(orderId);
            // return result.Status == "COMPLETED";
            
            // Always succeed in placeholder if orderId provided
            return !string.IsNullOrWhiteSpace(orderId);
        }
    }
}
