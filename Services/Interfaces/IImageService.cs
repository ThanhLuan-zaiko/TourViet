using TourViet.Models;
using TourViet.Models.DTOs;

namespace TourViet.Services.Interfaces
{
    /// <summary>
    /// Service interface for image processing and management operations.
    /// </summary>
    public interface IImageService
    {
        /// <summary>
        /// Processes and saves a single tour image file.
        /// </summary>
        /// <param name="tourId">The ID of the tour the image belongs to</param>
        /// <param name="file">The image file to process</param>
        /// <param name="sortOrder">The display order of the image</param>
        /// <returns>The created TourImage entity</returns>
        Task<TourImage> ProcessAndSaveTourImageAsync(Guid tourId, IFormFile file, int sortOrder);

        /// <summary>
        /// Processes and saves multiple tour image files.
        /// </summary>
        /// <param name="tourId">The ID of the tour the images belong to</param>
        /// <param name="files">Collection of image files to process</param>
        /// <param name="startingSortOrder">Starting sort order for the images</param>
        /// <returns>Collection of created TourImage entities</returns>
        Task<IEnumerable<TourImage>> ProcessMultipleImagesAsync(Guid tourId, IFormFileCollection files, int startingSortOrder = 1);

        /// <summary>
        /// Gets the current maximum sort order for a tour's images.
        /// </summary>
        /// <param name="tourId">The ID of the tour</param>
        /// <returns>The maximum sort order, or 0 if no images exist</returns>
        Task<int> GetMaxSortOrderAsync(Guid tourId);

        /// <summary>
        /// Deletes a tour image and its associated file.
        /// </summary>
        /// <param name="imageId">The ID of the image to delete</param>
        /// <returns>True if deletion was successful</returns>
        Task<bool> DeleteImageAsync(Guid imageId);
    }
}
