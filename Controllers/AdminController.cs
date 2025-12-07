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
        private readonly TripImageRepository _imageRepo;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AdminController(TripRepository tripRepo, TripImageRepository imageRepo, IWebHostEnvironment webHostEnvironment)
        {
            _tripRepo = tripRepo;
            _imageRepo = imageRepo;
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

            // Add trip first to get the TripID
            _tripRepo.Add(trip);

            // Handle image uploads and save to TripImages table as binary data
            if (model.Images != null && model.Images.Any())
            {
                var tripImages = new List<TripImage>();

                foreach (var image in model.Images)
                {
                    if (image.Length > 0)
                    {
                        // Read image into byte array
                        using (var memoryStream = new MemoryStream())
                        {
                            await image.CopyToAsync(memoryStream);
                            
                            // Create TripImage record with binary data
                            tripImages.Add(new TripImage
                            {
                                TripID = trip.TripID,
                                ImageData = memoryStream.ToArray(),
                                FileName = image.FileName,
                                ContentType = image.ContentType,
                                CreatedAt = DateTime.Now
                            });
                        }
                    }
                }

                // Save all images to database
                if (tripImages.Any())
                {
                    _imageRepo.AddRange(tripImages);
                }
            }

            TempData["SuccessMessage"] = "Trip added successfully!";
            return RedirectToAction("Dashboard");
        }
    }
}
