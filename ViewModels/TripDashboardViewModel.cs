namespace MVC_project.ViewModels
{
    public class TripDashboardViewModel
    {
        public int TripID { get; set; }
        public string Destination { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public string PackageType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool HasImage { get; set; }
        public int ImageCount { get; set; }
        public int AvailableRooms { get; set; }
        public List<TripDateVariation> DateVariations { get; set; } = new List<TripDateVariation>();

        // Computed property to get the closest upcoming date
        public DateTime ClosestStartDate
        {
            get
            {
                var today = DateTime.Today;
                
                // Get all possible dates (main date + variations)
                var allDates = new List<DateTime> { StartDate };
                allDates.AddRange(DateVariations.Select(d => d.StartDate));
                
                // Filter to only future dates and get the closest one
                var futureDates = allDates.Where(d => d >= today).OrderBy(d => d);
                
                return futureDates.Any() ? futureDates.First() : StartDate;
            }
        }

        public DateTime ClosestEndDate
        {
            get
            {
                var today = DateTime.Today;
                
                // Find which date range the closest start date belongs to
                if (ClosestStartDate == StartDate)
                {
                    return EndDate;
                }
                
                var matchingVariation = DateVariations.FirstOrDefault(d => d.StartDate == ClosestStartDate);
                return matchingVariation?.EndDate ?? EndDate;
            }
        }

        public int ClosestAvailableRooms
        {
            get
            {
                var today = DateTime.Today;
                
                // Find which date range the closest start date belongs to
                if (ClosestStartDate == StartDate)
                {
                    return AvailableRooms;
                }
                
                var matchingVariation = DateVariations.FirstOrDefault(d => d.StartDate == ClosestStartDate);
                return matchingVariation?.AvailableRooms ?? AvailableRooms;
            }
        }
    }

    public class TripDateVariation
    {
        public int TripDateID { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int AvailableRooms { get; set; }
    }
}
