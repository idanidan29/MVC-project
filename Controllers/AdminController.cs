using Microsoft.AspNetCore.Mvc;
using MVC_project.Data;
using MVC_project.Models;
using MVC_project.ViewModels;

namespace MVC_project.Controllers
{
    [Route("Admin")]
    public class AdminController : Controller
    {
        private readonly TripRepository _tripRepo;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AdminController(TripRepository tripRepo, IWebHostEnvironment webHostEnvironment)
        {
            _tripRepo = tripRepo;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: /Admin/Dashboard
        [HttpGet("Dashboard")]
        public IActionResult Dashboard()
        {
            return View();
        }

        // GET: /Admin/AddTrip
        [HttpGet("AddTrip")]
        public IActionResult AddTrip()
        {
            return View();
        }

        // POST: /Admin/AddTrip
        [HttpPost("AddTrip")]
        public async Task<IActionResult> AddTrip(AddTripViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Validate dates
            if (model.EndDate <= model.StartDate)
            {
                ModelState.AddModelError("EndDate", "End date must be after start date");
                return View(model);
            }

            // Validate discount
            if (model.DiscountPrice.HasValue && model.DiscountPrice >= model.Price)
            {
                ModelState.AddModelError("DiscountPrice", "Discount price must be less than regular price");
                return View(model);
            }

            var trip = new Trip
            {
                Destination = model.Destination,
                Country = model.Country,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                Price = model.Price,
                DiscountPrice = model.DiscountPrice,
                DiscountEndDate = model.DiscountEndDate,
                AvailableRooms = model.AvailableRooms,
                PackageType = model.PackageType,
                AgeLimit = model.AgeLimit,
                Description = model.Description,
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            // Handle image uploads (optional - for future implementation)
            if (model.Images != null && model.Images.Any())
            {
                // TODO: Save images to wwwroot/images/trips folder
                // For now, we'll just add the trip without images
            }

            _tripRepo.Add(trip);

            TempData["SuccessMessage"] = "Trip added successfully!";
            return RedirectToAction("Dashboard");
        }
    }
}
