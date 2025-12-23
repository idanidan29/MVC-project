namespace MVC_project.ViewModels
{
    public class UserTripViewModel
    {
        public int UserTripID { get; set; }  // Cart entry ID for deletion
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

        // Quantity selected by the user (cart item)
        public int Quantity { get; set; } = 1;
        
        // Date variations for this trip
        public List<DateVariationInfo> DateVariations { get; set; } = new List<DateVariationInfo>();
        
        // List of dates user has selected for this trip
        public List<UserSelectedDateInfo> UserSelectedDates { get; set; } = new List<UserSelectedDateInfo>();
        
        // Computed properties
        public decimal FinalPrice => (DiscountPrice.HasValue && DiscountEndDate.HasValue && DiscountEndDate > DateTime.Now) 
            ? DiscountPrice.Value 
            : Price;
        
        public bool HasDiscount => DiscountPrice.HasValue && DiscountEndDate.HasValue && DiscountEndDate > DateTime.Now;
        
        public int DurationDays => (EndDate - StartDate).Days;
        
        // Get date info by index
        public DateInfo GetDateByIndex(int selectedDateIndex)
        {
            if (selectedDateIndex == -1)
            {
                // Return main date
                return new DateInfo
                {
                    StartDate = StartDate,
                    EndDate = EndDate,
                    AvailableRooms = AvailableRooms,
                    IsMainDate = true
                };
            }
            else if (selectedDateIndex >= 0 && selectedDateIndex < DateVariations.Count)
            {
                // Return selected variation
                var variation = DateVariations[selectedDateIndex];
                return new DateInfo
                {
                    StartDate = variation.StartDate,
                    EndDate = variation.EndDate,
                    AvailableRooms = variation.AvailableRooms,
                    IsMainDate = false
                };
            }
            else
            {
                // Fallback to main date
                return new DateInfo
                {
                    StartDate = StartDate,
                    EndDate = EndDate,
                    AvailableRooms = AvailableRooms,
                    IsMainDate = true
                };
            }
        }
    }
    
    public class UserSelectedDateInfo
    {
        public int UserTripID { get; set; }
        public int SelectedDateIndex { get; set; }
        public int Quantity { get; set; }
    }
    
    public class DateVariationInfo
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int AvailableRooms { get; set; }
    }
    
    public class DateInfo
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int AvailableRooms { get; set; }
        public bool IsMainDate { get; set; }
        public int DurationDays => (EndDate - StartDate).Days;
    }
}
