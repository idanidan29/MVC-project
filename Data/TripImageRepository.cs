using MVC_project.Models;

namespace MVC_project.Data
{
    public class TripImageRepository
    {
        private readonly AppDbContext _context;

        public TripImageRepository(AppDbContext context)
        {
            _context = context;
        }

        public void Add(TripImage image)
        {
            _context.TripImages.Add(image);
            _context.SaveChanges();
        }

        public void AddRange(IEnumerable<TripImage> images)
        {
            _context.TripImages.AddRange(images);
            _context.SaveChanges();
        }

        public IEnumerable<TripImage> GetByTripId(int tripId)
        {
            return _context.TripImages.Where(i => i.TripID == tripId).ToList();
        }

        public TripImage? GetById(int imageId)
        {
            return _context.TripImages.FirstOrDefault(i => i.ImageID == imageId);
        }

        public void Delete(int imageId)
        {
            var image = GetById(imageId);
            if (image != null)
            {
                _context.TripImages.Remove(image);
                _context.SaveChanges();
            }
        }

        public void DeleteByTripId(int tripId)
        {
            var images = GetByTripId(tripId);
            _context.TripImages.RemoveRange(images);
            _context.SaveChanges();
        }
    }
}
