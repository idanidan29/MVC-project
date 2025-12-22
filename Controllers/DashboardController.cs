using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MVC_project.Data;
using MVC_project.ViewModels;

namespace MVC_project.Controllers
{
    [Route("Dashboard")]
    public class DashboardController : Controller
    {
        private readonly TripRepository _tripRepo;
        private readonly TripImageRepository _imageRepo;
        private readonly TripDateRepository _tripDateRepo;

        public DashboardController(TripRepository tripRepo, TripImageRepository imageRepo, TripDateRepository tripDateRepo)
        {
            _tripRepo = tripRepo;
            _imageRepo = imageRepo;
            _tripDateRepo = tripDateRepo;
        }

        // GET: /Dashboard or /Dashboard/Index
        [HttpGet("")]
        [HttpGet("Index")]
        public IActionResult Index()
        {
            // Get all active trips
            var trips = _tripRepo.GetActiveTrips();

            // Map to view models with image check
            var tripViewModels = trips.Select(trip => {
                var images = _imageRepo.GetByTripId(trip.TripID);
                var dates = _tripDateRepo.GetByTripId(trip.TripID);
                
                return new TripDashboardViewModel
                {
                    TripID = trip.TripID,
                    Destination = trip.Destination,
                    Country = trip.Country,
                    StartDate = trip.StartDate,
                    EndDate = trip.EndDate,
                    Price = trip.Price,
                    DiscountPrice = trip.DiscountPrice,
                    PackageType = trip.PackageType,
                    Description = trip.Description,
                    HasImage = images.Any(),
                    ImageCount = images.Count(),
                    AvailableRooms = trip.AvailableRooms,
                    DateVariations = dates.Select(d => new TripDateVariation
                    {
                        TripDateID = d.TripDateID,
                        StartDate = d.StartDate,
                        EndDate = d.EndDate,
                        AvailableRooms = d.AvailableRooms
                    }).ToList()
                };
            }).ToList();

            return View(tripViewModels);
        }

        // GET: /Dashboard/Search - displays the search page
        [HttpGet("Search")]
        public IActionResult Search()
        {
            // Redirect to filter controller for actual filtering
            // This action just loads the initial view
            return RedirectToAction("Search", "Filter");
        }

        // GET: /Dashboard/GetImage?tripId=1
        [HttpGet("GetImage")]
        public IActionResult GetImage(int tripId)
        {
            // Get first image for the trip
            var image = _imageRepo.GetByTripId(tripId).FirstOrDefault();
            
            if (image == null)
                return NotFound();

            return File(image.ImageData, image.ContentType ?? "image/jpeg");
        }

        // GET: /Dashboard/GetAllImages?tripId=1
        [HttpGet("GetAllImages")]
        public IActionResult GetAllImages(int tripId)
        {
            var images = _imageRepo.GetByTripId(tripId);
            
            if (!images.Any())
                return Ok(new { images = new List<object>() });

            var imageList = images.Select(img => new
            {
                imageId = img.ImageID,
                imageUrl = Url.Action("GetImageById", "Dashboard", new { imageId = img.ImageID })
            }).ToList();

            return Ok(new { images = imageList });
        }

        // GET: /Dashboard/GetImageById?imageId=1
        [HttpGet("GetImageById")]
        public IActionResult GetImageById(int imageId)
        {
            var image = _imageRepo.GetById(imageId);
            
            if (image == null)
                return NotFound();

            return File(image.ImageData, image.ContentType ?? "image/jpeg");
        }
    }
}
