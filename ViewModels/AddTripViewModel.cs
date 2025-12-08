using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace MVC_project.ViewModels
{
    public class AddTripViewModel
    {
        [Required(ErrorMessage = "Destination is required")]
        [StringLength(100, ErrorMessage = "Destination cannot exceed 100 characters")]
        public string Destination { get; set; } = string.Empty;

        [Required(ErrorMessage = "Country is required")]
        [StringLength(100, ErrorMessage = "Country cannot exceed 100 characters")]
        public string Country { get; set; } = string.Empty;

        [Required(ErrorMessage = "Start date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "End date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(0, 999999.99, ErrorMessage = "Price must be a non-negative value up to 999,999.99")]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        [Range(0, 999999.99, ErrorMessage = "Discount price must be a non-negative value up to 999,999.99")]
        [DataType(DataType.Currency)]
        [Display(Name = "Discount Price (Optional)")]
        public decimal? DiscountPrice { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Discount End Date (Optional)")]
        public DateTime? DiscountEndDate { get; set; }

        [Required(ErrorMessage = "Number of available rooms is required")]
        [Range(0, 10000, ErrorMessage = "Available rooms must be between 0 and 10,000")]
        [Display(Name = "Available Rooms")]
        public int AvailableRooms { get; set; }

        [Required(ErrorMessage = "Package type is required")]
        [Display(Name = "Package Type")]
        public string PackageType { get; set; } = string.Empty;

        [Range(0, 120, ErrorMessage = "Age limit must be between 0 and 120")]
        [Display(Name = "Age Limit (Optional)")]
        public int? AgeLimit { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [StringLength(5000, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 5000 characters")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Trip Images (Optional)")]
        public List<IFormFile>? Images { get; set; }
    }
}
