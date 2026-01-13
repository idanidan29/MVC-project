using MVC_project.Models;

namespace MVC_project.Data
{
    /// <summary>
    /// Repository for TripImage entity operations.
    /// Manages trip photo storage and retrieval. Images are stored as binary data (VARBINARY)
    /// in the database rather than as files on disk for easier deployment and backup.
    /// </summary>
    public class TripImageRepository
    {
        private readonly AppDbContext _context;

        public TripImageRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Adds a single image to the database.
        /// The image data is stored as byte array in ImageData column.
        /// SaveChanges() generates the auto-increment ImageID.
        /// </summary>
        public void Add(TripImage image)
        {
            _context.TripImages.Add(image);  // Add to change tracker
            _context.SaveChanges();           // Persist to database and generate ImageID
        }

        /// <summary>
        /// Adds multiple images in a single transaction.
        /// More efficient than calling Add() multiple times because it uses one database round-trip.
        /// Used when admin uploads multiple photos at once during trip creation.
        /// </summary>
        public void AddRange(IEnumerable<TripImage> images)
        {
            _context.TripImages.AddRange(images);  // Add all images to change tracker
            _context.SaveChanges();                 // Single SaveChanges for all images (one transaction)
        }

        /// <summary>
        /// Retrieves all images for a specific trip.
        /// Returns image metadata and binary data.
        /// ToList() executes query and loads all images into memory.
        /// </summary>
        public IEnumerable<TripImage> GetByTripId(int tripId)
        {
            return _context.TripImages.Where(i => i.TripID == tripId).ToList();  // SQL: SELECT * FROM TripImages WHERE TripID = @tripId
        }

        /// <summary>
        /// Retrieves a single image by its ID.
        /// Used when downloading/viewing a specific image.
        /// Returns null if image doesn't exist.
        /// </summary>
        public TripImage? GetById(int imageId)
        {
            return _context.TripImages.FirstOrDefault(i => i.ImageID == imageId);  // Find first matching or return null
        }

        /// <summary>
        /// Deletes a single image from the database.
        /// Admin uses this to remove unwanted photos from a trip.
        /// Validation in AdminController prevents deleting the last image.
        /// </summary>
        public void Delete(int imageId)
        {
            var image = GetById(imageId);  // Retrieve image entity
            if (image != null)              // Check if exists
            {
                _context.TripImages.Remove(image);  // Mark for deletion
                _context.SaveChanges();              // Execute DELETE SQL
            }
        }

        /// <summary>
        /// Deletes all images for a specific trip.
        /// Called when admin deletes a trip entirely.
        /// RemoveRange is more efficient than deleting images one by one.
        /// </summary>
        public void DeleteByTripId(int tripId)
        {
            var images = GetByTripId(tripId);          // Get all images for this trip
            _context.TripImages.RemoveRange(images);   // Mark all for deletion
            _context.SaveChanges();                     // Execute DELETE in single transaction
        }
    }
}
