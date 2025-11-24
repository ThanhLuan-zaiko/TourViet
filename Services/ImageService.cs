using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using TourViet.Data;
using TourViet.Models;
using TourViet.Services.Interfaces;

namespace TourViet.Services
{
    /// <summary>
    /// Service for handling image processing and storage operations.
    /// </summary>
    public class ImageService : IImageService
    {
        private readonly TourBookingDbContext _context;
        private const string UploadsBasePath = "Uploads";

        public ImageService(TourBookingDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public async Task<TourImage> ProcessAndSaveTourImageAsync(Guid tourId, IFormFile file, int sortOrder)
        {
            if (file.Length == 0)
                throw new ArgumentException("File is empty", nameof(file));

            // Create upload directory if it doesn't exist
            var uploadDir = Path.Combine(UploadsBasePath, tourId.ToString());
            if (!Directory.Exists(uploadDir))
            {
                Directory.CreateDirectory(uploadDir);
            }

            // Generate unique filename and save as WebP
            var fileName = $"{Guid.NewGuid()}.webp";
            var filePath = Path.Combine(uploadDir, fileName);

            using (var image = await Image.LoadAsync(file.OpenReadStream()))
            {
                await image.SaveAsWebpAsync(filePath);
            }

            // Create TourImage entity
            var tourImage = new TourImage
            {
                TourID = tourId,
                Url = $"/{UploadsBasePath}/{tourId}/{fileName}",
                MimeType = "image/webp",
                SortOrder = sortOrder,
                FileName = fileName,
                FileSize = (int)file.Length,
                UploadedAt = DateTime.UtcNow
            };

            return tourImage;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<TourImage>> ProcessMultipleImagesAsync(Guid tourId, IFormFileCollection files, int startingSortOrder = 1)
        {
            var tourImages = new List<TourImage>();
            int currentSortOrder = startingSortOrder;

            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    var tourImage = await ProcessAndSaveTourImageAsync(tourId, file, currentSortOrder);
                    tourImages.Add(tourImage);
                    currentSortOrder++;
                }
            }

            return tourImages;
        }

        /// <inheritdoc/>
        public async Task<int> GetMaxSortOrderAsync(Guid tourId)
        {
            var maxSortOrder = await _context.TourImages
                .Where(ti => ti.TourID == tourId)
                .Select(ti => (int?)ti.SortOrder)
                .MaxAsync();

            return maxSortOrder ?? 0;
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteImageAsync(Guid imageId)
        {
            var image = await _context.TourImages.FindAsync(imageId);
            if (image == null)
                return false;

            // Try to delete file from file system
            var filePath = Path.Combine(UploadsBasePath, image.TourID.ToString(), image.FileName ?? "");
            
            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (Exception ex)
                {
                    // Log warning but continue with database deletion
                    Console.WriteLine($"Warning: Could not delete physical file {filePath}: {ex.Message}");
                }
            }
            else
            {
                // File doesn't exist, log warning but continue
                Console.WriteLine($"Warning: Physical file not found at {filePath}. Removing database record only.");
            }

            // Remove from database (even if file deletion failed to avoid orphan records)
            _context.TourImages.Remove(image);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
