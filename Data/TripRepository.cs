using MVC_project.Models;

namespace MVC_project.Data
{
    public class TripRepository
    {
        private readonly AppDbContext _context;

        public TripRepository(AppDbContext context)
        {
            _context = context;
        }

        public void Add(Trip trip)
        {
            _context.Trips.Add(trip);
            _context.SaveChanges();
        }

        public Trip? GetById(int id)
        {
            return _context.Trips.FirstOrDefault(t => t.TripID == id);
        }

        public IEnumerable<Trip> GetAll()
        {
            return _context.Trips.ToList();
        }

        public IEnumerable<Trip> GetActiveTrips()
        {
            return _context.Trips.Where(t => t.IsActive).ToList();
        }

        public void Update(Trip trip)
        {
            _context.Trips.Update(trip);
            _context.SaveChanges();
        }

        public void Delete(int id)
        {
            var trip = GetById(id);
            if (trip != null)
            {
                _context.Trips.Remove(trip);
                _context.SaveChanges();
            }
        }

        public void SoftDelete(int id)
        {
            var trip = GetById(id);
            if (trip != null)
            {
                trip.IsActive = false;
                Update(trip);
            }
        }

        public bool ToggleVisibility(int id)
        {
            var trip = GetById(id);
            if (trip != null)
            {
                trip.IsVisible = !trip.IsVisible;
                Update(trip);
                return trip.IsVisible;
            }
            return false;
        }
    }
}
