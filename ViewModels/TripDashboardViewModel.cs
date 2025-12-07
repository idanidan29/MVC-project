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
    }
}
