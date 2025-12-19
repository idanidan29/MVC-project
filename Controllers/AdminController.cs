using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using MVC_project.Data;
using MVC_project.Models;
using MVC_project.ViewModels;
using MVC_project.Services;

namespace MVC_project.Controllers
{
    [Route("Admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly TripRepository _tripRepo;
        private readonly TripImageRepository _imageRepo;
        private readonly UserRepository _userRepo;
        private readonly UserTripRepository _userTripRepo;
        private readonly PasswordService _passwordService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AdminController(TripRepository tripRepo, TripImageRepository imageRepo, UserRepository userRepo, UserTripRepository userTripRepo, PasswordService passwordService, IWebHostEnvironment webHostEnvironment)
        {
            _tripRepo = tripRepo;
            _imageRepo = imageRepo;
            _userRepo = userRepo;
            _userTripRepo = userTripRepo;
            _passwordService = passwordService;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: /Admin/Dashboard
        [HttpGet("Dashboard")]
        public IActionResult Dashboard(string? name, string? email)
        {
            var users = string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(email)
                ? _userRepo.GetAll()
                : _userRepo.Search(name, email);

            var vm = new AdminDashboardViewModel
            {
                SearchName = name,
                SearchEmail = email,
            };

            foreach (var u in users)
            {
                vm.Users.Add(new AdminUserItem
                {
                    User = u,
                    BookingsCount = _userTripRepo.GetCount(u.email)
                });
            }

            return View(vm);
        }

        // GET: /Admin/UserDetails?email=foo@bar
        [HttpGet("UserDetails")]
        public IActionResult UserDetails(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest("Email is required");
            }

            var user = _userRepo.GetByEmail(email);
            if (user == null)
            {
                return NotFound();
            }

            var bookings = _userTripRepo.GetByUserEmail(email).ToList();
            var vm = new UserDetailsViewModel
            {
                User = user,
                Bookings = bookings
            };

            return PartialView("_UserDetailsPartial", vm);
        }

        // POST: /Admin/DeleteUser
        [HttpPost("DeleteUser")]
        public IActionResult DeleteUser(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(new { success = false, message = "Email is required" });
            }

            var user = _userRepo.GetByEmail(email);
            if (user == null)
            {
                return NotFound(new { success = false, message = "User not found" });
            }

            // Prevent deleting self
            if (user.email == User.Identity.Name)
            {
                return BadRequest(new { success = false, message = "Cannot delete your own account" });
            }

            _userRepo.Delete(email);
            return Ok(new { success = true, message = "User deleted successfully" });
        }

        // GET: /Admin/EditUser?email=foo@bar
        [HttpGet("EditUser")]
        public IActionResult EditUser(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest("Email is required");
            }

            var user = _userRepo.GetByEmail(email);
            if (user == null)
            {
                return NotFound();
            }

            return PartialView("_EditUserPartial", user);
        }

        // POST: /Admin/UpdateUser
        [HttpPost("UpdateUser")]
        public IActionResult UpdateUser(string email, string firstName, string lastName, bool isAdmin)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(new { success = false, message = "Email is required" });
            }

            var user = _userRepo.GetByEmail(email);
            if (user == null)
            {
                return NotFound(new { success = false, message = "User not found" });
            }

            // Prevent demoting self from admin
            if (user.email == User.Identity.Name && user.admin && !isAdmin)
            {
                return BadRequest(new { success = false, message = "Cannot demote yourself from admin" });
            }

            user.first_name = firstName?.Trim() ?? user.first_name;
            user.last_name = lastName?.Trim() ?? user.last_name;
            user.admin = isAdmin;

            _userRepo.Update(user);
            return Ok(new { success = true, message = "User updated successfully" });
        }

        // GET: /Admin/AddUserModal
        [HttpGet("AddUserModal")]
        public IActionResult AddUserModal()
        {
            return PartialView("_AddUserPartial", new AddUserViewModel());
        }

        // POST: /Admin/CreateUser
        [HttpPost("CreateUser")]
        public IActionResult CreateUser(AddUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Invalid form data" });
            }

            if (_userRepo.EmailExists(model.Email))
            {
                return BadRequest(new { success = false, message = "Email already exists" });
            }

            var user = new User
            {
                email = model.Email,
                first_name = model.FirstName,
                last_name = model.LastName,
                passwordHash = _passwordService.HashPassword(model.Password),
                admin = model.IsAdmin
            };

            _userRepo.Add(user);
            return Ok(new { success = true, message = "User created successfully" });
        }
        // GET: /Admin/AddTrip
        [HttpGet("AddTrip")]
        public IActionResult AddTrip()
        {
            return View();
        }

        // POST: /Admin/AddTrip
        [HttpPost("AddTrip")]
        [RequestSizeLimit(104_857_600)] // 100 MB per-request limit
        [RequestFormLimits(MultipartBodyLengthLimit = 104_857_600)]
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

        // GET: /Admin/EditTrip/5
        [HttpGet("EditTrip/{id}")]
        public IActionResult EditTrip(int id)
        {
            var trip = _tripRepo.GetById(id);
            if (trip == null)
                return NotFound();

            var model = new AddTripViewModel
            {
                Destination = trip.Destination,
                Country = trip.Country,
                StartDate = trip.StartDate,
                EndDate = trip.EndDate,
                Price = trip.Price,
                DiscountPrice = trip.DiscountPrice,
                DiscountEndDate = trip.DiscountEndDate,
                AvailableRooms = trip.AvailableRooms,
                PackageType = trip.PackageType,
                AgeLimit = trip.AgeLimit,
                Description = trip.Description
            };

            ViewBag.TripId = id;
            ViewBag.ExistingImages = _imageRepo.GetByTripId(id).ToList();
            return View(model);
        }

        // POST: /Admin/EditTrip/5
        [HttpPost("EditTrip/{id}")]
        [RequestSizeLimit(104_857_600)]
        [RequestFormLimits(MultipartBodyLengthLimit = 104_857_600)]
        public async Task<IActionResult> EditTrip(int id, AddTripViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.TripId = id;
                ViewBag.ExistingImages = _imageRepo.GetByTripId(id).ToList();
                return View(model);
            }

            var trip = _tripRepo.GetById(id);
            if (trip == null)
                return NotFound();

            // Validate dates
            if (model.EndDate <= model.StartDate)
            {
                ModelState.AddModelError("EndDate", "End date must be after start date");
                ViewBag.TripId = id;
                ViewBag.ExistingImages = _imageRepo.GetByTripId(id).ToList();
                return View(model);
            }

            // Validate discount
            if (model.DiscountPrice.HasValue && model.DiscountPrice >= model.Price)
            {
                ModelState.AddModelError("DiscountPrice", "Discount price must be less than regular price");
                ViewBag.TripId = id;
                ViewBag.ExistingImages = _imageRepo.GetByTripId(id).ToList();
                return View(model);
            }

            // Update trip properties
            trip.Destination = model.Destination;
            trip.Country = model.Country;
            trip.StartDate = model.StartDate;
            trip.EndDate = model.EndDate;
            trip.Price = model.Price;
            trip.DiscountPrice = model.DiscountPrice;
            trip.DiscountEndDate = model.DiscountEndDate;
            trip.AvailableRooms = model.AvailableRooms;
            trip.PackageType = model.PackageType;
            trip.AgeLimit = model.AgeLimit;
            trip.Description = model.Description;

            _tripRepo.Update(trip);

            // Handle new image uploads
            if (model.Images != null && model.Images.Any())
            {
                var tripImages = new List<TripImage>();

                foreach (var image in model.Images)
                {
                    if (image.Length > 0)
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            await image.CopyToAsync(memoryStream);
                            
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

                if (tripImages.Any())
                {
                    _imageRepo.AddRange(tripImages);
                }
            }

            TempData["SuccessMessage"] = "Trip updated successfully!";
            return RedirectToAction("Dashboard");
        }

        // POST: /Admin/DeleteTrip
        [HttpPost("DeleteTrip")]
        public IActionResult DeleteTrip(int tripId)
        {
            if (tripId <= 0)
                return BadRequest(new { success = false, message = "Invalid trip ID" });

            var trip = _tripRepo.GetById(tripId);
            if (trip == null)
                return NotFound(new { success = false, message = "Trip not found" });

            // Delete associated images first
            _imageRepo.DeleteByTripId(tripId);

            // Delete the trip
            _tripRepo.Delete(tripId);

            return Ok(new { success = true, message = "Trip deleted successfully" });
        }

        // POST: /Admin/DeleteImage
        [HttpPost("DeleteImage")]
        public IActionResult DeleteImage(int imageId)
        {
            var image = _imageRepo.GetById(imageId);
            if (image == null)
                return NotFound(new { success = false, message = "Image not found" });

            _imageRepo.Delete(imageId);
            return Ok(new { success = true, message = "Image deleted successfully" });
        }
    }
}
