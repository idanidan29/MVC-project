using Microsoft.EntityFrameworkCore;
using MVC_project.Models;

namespace MVC_project.Data
{
    public class FeedbackRepository
    {
        private readonly AppDbContext _context;

        public FeedbackRepository(AppDbContext context)
        {
            _context = context;
        }

        public UserFeedback Add(UserFeedback feedback)
        {
            _context.UserFeedback.Add(feedback);
            _context.SaveChanges();
            return feedback;
        }

        public IEnumerable<UserFeedback> GetApprovedFeedback()
        {
            return _context.UserFeedback
                .Include(f => f.User)
                .Where(f => f.IsApproved)
                .OrderByDescending(f => f.IsFeatured)
                .ThenByDescending(f => f.CreatedAt)
                .ToList();
        }

        public IEnumerable<UserFeedback> GetTopFeaturedFeedback(int count = 10)
        {
            return _context.UserFeedback
                .Include(f => f.User)
                .Where(f => f.IsApproved)
                .OrderByDescending(f => f.Rating)
                .ThenByDescending(f => f.CreatedAt)
                .Take(count)
                .ToList();
        }

        public IEnumerable<UserFeedback> GetByUserId(int userId)
        {
            return _context.UserFeedback
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToList();
        }

        public bool HasUserSubmittedFeedback(int userId)
        {
            return _context.UserFeedback.Any(f => f.UserId == userId);
        }

        public UserFeedback? GetByUserIdSingle(int userId)
        {
            return _context.UserFeedback
                .Include(f => f.User)
                .FirstOrDefault(f => f.UserId == userId);
        }

        public UserFeedback? GetById(int feedbackId)
        {
            return _context.UserFeedback
                .Include(f => f.User)
                .FirstOrDefault(f => f.FeedbackID == feedbackId);
        }

        public void Update(UserFeedback feedback)
        {
            _context.UserFeedback.Update(feedback);
            _context.SaveChanges();
        }

        public void Delete(int feedbackId)
        {
            var feedback = GetById(feedbackId);
            if (feedback != null)
            {
                _context.UserFeedback.Remove(feedback);
                _context.SaveChanges();
            }
        }
    }
}
