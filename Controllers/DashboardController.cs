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

        public DashboardController(TripRepository tripRepo, TripImageRepository imageRepo)
        {
            _tripRepo = tripRepo;
            _imageRepo = imageRepo;
        }

        // GET: /Dashboard or /Dashboard/Index
        [HttpGet("")]
        [HttpGet("Index")]
        public IActionResult Index()
        {
            // Get all active trips
            var trips = _tripRepo.GetActiveTrips();

            // Map to view models with image check
            var tripViewModels = trips.Select(trip => new TripDashboardViewModel
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
                HasImage = _imageRepo.GetByTripId(trip.TripID).Any()
            }).ToList();

            return View(tripViewModels);
        }

        // GET: /Dashboard/Search - filtered and searchable trips
        [HttpGet("Search")]
        public IActionResult Search()
        {
            // Get all active trips
            var trips = _tripRepo.GetActiveTrips();

            // Map to view models with image check
            var tripViewModels = trips.Select(trip => new TripDashboardViewModel
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
                HasImage = _imageRepo.GetByTripId(trip.TripID).Any()
            }).ToList();

            return View(tripViewModels);
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
    }
}
