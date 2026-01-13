using Microsoft.EntityFrameworkCore;
using MVC_project.Models;

namespace MVC_project.Data
{
    /// <summary>
    /// Repository for UserFeedback entity operations.
    /// Manages general website feedback/testimonials from customers.
    /// Different from TripRating - this is about overall experience, not specific trips.
    /// Admin can approve/feature testimonials to display on homepage.
    /// </summary>
    public class FeedbackRepository
    {
        private readonly AppDbContext _context;

        public FeedbackRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Creates new feedback entry from a customer.
        /// IsApproved defaults to false - admin must approve before it shows publicly.
        /// Returns the feedback with generated FeedbackID.
        /// </summary>
        public UserFeedback Add(UserFeedback feedback)
        {
            _context.UserFeedback.Add(feedback);  // Add to change tracker
            _context.SaveChanges();                // Persist and generate ID
            return feedback;                       // Return with populated FeedbackID
        }

        /// <summary>
        /// Retrieves all approved feedback for public display.
        /// 
        /// Sorting logic:
        /// 1. Featured feedback first (IsFeatured = true appears at top)
        /// 2. Within each group (featured/not featured), newest first
        /// 
        /// This ensures best testimonials are prominently displayed.
        /// </summary>
        public IEnumerable<UserFeedback> GetApprovedFeedback()
        {
            return _context.UserFeedback
                .Include(f => f.User)                   // Eager load user for displaying name
                .Where(f => f.IsApproved)               // Only show approved feedback
                .OrderByDescending(f => f.IsFeatured)   // Featured first (true > false in boolean sort)
                .ThenByDescending(f => f.CreatedAt)     // Then newest first within each group
                .ToList();                              // Execute query
        }

        /// <summary>
        /// Retrieves top-rated approved feedback for homepage testimonials.
        /// 
        /// Why not use IsFeatured?
        /// This method sorts by rating (5-star reviews first) then recency.
        /// Used for different display purposes than GetApprovedFeedback.
        /// Take(count) limits results to prevent loading too much data.
        /// </summary>
        public IEnumerable<UserFeedback> GetTopFeaturedFeedback(int count = 10)
        {
            return _context.UserFeedback
                .Include(f => f.User)                 // Load user data
                .Where(f => f.IsApproved)             // Only approved
                .OrderByDescending(f => f.Rating)     // Highest ratings first (5 stars > 4 stars)
                .ThenByDescending(f => f.CreatedAt)   // Most recent within same rating
                .Take(count)                          // Limit to specified count (SQL: TOP N)
                .ToList();
        }

        /// <summary>
        /// Retrieves all feedback submitted by a specific user.
        /// Users can see their own feedback history (approved or pending).
        /// Newest first for better UX.
        /// </summary>
        public IEnumerable<UserFeedback> GetByUserId(int userId)
        {
            return _context.UserFeedback
                .Where(f => f.UserId == userId)       // Filter to specific user
                .OrderByDescending(f => f.CreatedAt)   // Newest first
                .ToList();
        }

        /// <summary>
        /// Checks if a user has already submitted feedback.
        /// Any() is more efficient than Count() > 0 or FirstOrDefault() != null
        /// because it stops searching as soon as it finds one match.
        /// Can be used to limit users to one feedback submission.
        /// </summary>
        public bool HasUserSubmittedFeedback(int userId)
        {
            return _context.UserFeedback.Any(f => f.UserId == userId);  // Returns true if at least one exists
        }

        /// <summary>
        /// Gets a user's single feedback entry (assumes one feedback per user).
        /// If users are limited to one feedback, this retrieves their submission.
        /// Returns null if user hasn't submitted feedback.
        /// </summary>
        public UserFeedback? GetByUserIdSingle(int userId)
        {
            return _context.UserFeedback
                .Include(f => f.User)                    // Load user data
                .FirstOrDefault(f => f.UserId == userId);  // Get first (should be only one)
        }

        /// <summary>
        /// Retrieves feedback by its ID with user information.
        /// Used in admin panel for viewing/editing specific feedback.
        /// </summary>
        public UserFeedback? GetById(int feedbackId)
        {
            return _context.UserFeedback
                .Include(f => f.User)                         // Eager load user
                .FirstOrDefault(f => f.FeedbackID == feedbackId);  // Find by primary key
        }

        /// <summary>
        /// Updates feedback (admin approval, featuring, or user edit).
        /// Admin can change IsApproved or IsFeatured flags.
        /// User can edit their comment (if allowed by business rules).
        /// </summary>
        public void Update(UserFeedback feedback)
        {
            _context.UserFeedback.Update(feedback);  // Mark as modified
            _context.SaveChanges();                   // Persist changes
        }

        /// <summary>
        /// Deletes feedback entry (admin moderation).
        /// Used to remove inappropriate or spam feedback.
        /// </summary>
        public void Delete(int feedbackId)
        {
            var feedback = GetById(feedbackId);  // Retrieve entity
            if (feedback != null)                 // Verify exists
            {
                _context.UserFeedback.Remove(feedback);  // Mark for deletion
                _context.SaveChanges();                   // Execute DELETE
            }
        }
    }
}
