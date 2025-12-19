namespace MVC_project.ViewModels
{
    public class UserTripViewModel
    {
        public int TripID { get; set; }
        public string Destination { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public DateTime? DiscountEndDate { get; set; }
        public string PackageType { get; set; } = string.Empty;
        public int AvailableRooms { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<byte[]> Images { get; set; } = new List<byte[]>();
        
        // Computed properties
        public decimal FinalPrice => (DiscountPrice.HasValue && DiscountEndDate.HasValue && DiscountEndDate > DateTime.Now) 
            ? DiscountPrice.Value 
            : Price;
        
        public bool HasDiscount => DiscountPrice.HasValue && DiscountEndDate.HasValue && DiscountEndDate > DateTime.Now;
        
        public int DurationDays => (EndDate - StartDate).Days;
    }
}
