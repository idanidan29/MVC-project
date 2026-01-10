using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MVC_project.Data;
using MVC_project.Models;
using System.Security.Claims;

namespace MVC_project.Controllers
{
    public class InfoController : Controller
    {
        private readonly FeedbackRepository _feedbackRepo;
        private readonly UserRepository _userRepo;

        public InfoController(FeedbackRepository feedbackRepo, UserRepository userRepo)
        {
            _feedbackRepo = feedbackRepo;
            _userRepo = userRepo;
        }

        [HttpGet("")]
        [HttpGet("Info")]
        [HttpGet("Info/Index")]
        public IActionResult Index()
        {
            var featuredFeedback = _feedbackRepo.GetTopFeaturedFeedback(10);
            
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            bool hasSubmitted = false;
            UserFeedback? userFeedback = null;
            
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
            {
                userFeedback = _feedbackRepo.GetByUserIdSingle(userId);
                hasSubmitted = userFeedback != null;
            }

            ViewBag.FeaturedFeedback = featuredFeedback;
            ViewBag.HasSubmittedFeedback = hasSubmitted;
            ViewBag.UserFeedback = userFeedback;
            ViewBag.IsAuthenticated = User.Identity?.IsAuthenticated ?? false;
            
            return View();
        }

        [HttpPost]
        [Authorize]
        public IActionResult SubmitFeedback([FromBody] FeedbackSubmission submission)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Json(new { success = false, message = "User not authenticated" });
            }

            // Check if user already submitted feedback
            if (_feedbackRepo.HasUserSubmittedFeedback(userId))
            {
                return Json(new { success = false, message = "You have already submitted feedback" });
            }

            if (submission.Rating < 1 || submission.Rating > 5)
            {
                return Json(new { success = false, message = "Rating must be between 1 and 5" });
            }

            if (string.IsNullOrWhiteSpace(submission.FeedbackText) || submission.FeedbackText.Length > 1000)
            {
                return Json(new { success = false, message = "Feedback must be between 1 and 1000 characters" });
            }

            var feedback = new UserFeedback
            {
                UserId = userId,
                Rating = submission.Rating,
                FeedbackText = submission.FeedbackText.Trim(),
                CreatedAt = DateTime.UtcNow,
                IsApproved = true,
                IsFeatured = submission.Rating == 5
            };

            _feedbackRepo.Add(feedback);

            return Json(new { success = true, message = "Thank you for sharing your experience with us!" });
        }

        [HttpPost]
        [Authorize]
        public IActionResult UpdateFeedback([FromBody] FeedbackSubmission submission)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Json(new { success = false, message = "User not authenticated" });
            }

            var existingFeedback = _feedbackRepo.GetByUserIdSingle(userId);
            if (existingFeedback == null)
            {
                return Json(new { success = false, message = "No feedback found to update" });
            }

            if (submission.Rating < 1 || submission.Rating > 5)
            {
                return Json(new { success = false, message = "Rating must be between 1 and 5" });
            }

            if (string.IsNullOrWhiteSpace(submission.FeedbackText) || submission.FeedbackText.Length > 1000)
            {
                return Json(new { success = false, message = "Feedback must be between 1 and 1000 characters" });
            }

            existingFeedback.Rating = submission.Rating;
            existingFeedback.FeedbackText = submission.FeedbackText.Trim();
            existingFeedback.IsFeatured = submission.Rating == 5;
            existingFeedback.CreatedAt = DateTime.UtcNow;

            _feedbackRepo.Update(existingFeedback);

            return Json(new { success = true, message = "Your feedback has been updated successfully!" });
        }
    }

    public class FeedbackSubmission
    {
        public int Rating { get; set; }
        public string FeedbackText { get; set; } = string.Empty;
    }
}
