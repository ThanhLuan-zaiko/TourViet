using TourViet.Models;
using TourViet.Models.DTOs;

namespace TourViet.Services.Interfaces
{
    /// <summary>
    /// Service interface for tour business logic operations.
    /// </summary>
    public interface ITourService
    {
        /// <summary>
        /// Creates a new tour with all related entities.
        /// </summary>
        /// <param name="tourDto">The tour creation data transfer object</param>
        /// <param name="imageFiles">Optional collection of image files to upload</param>
        /// <returns>The created Tour entity</returns>
        Task<Tour> CreateTourAsync(TourCreateDto tourDto, IFormFileCollection? imageFiles);

        /// <summary>
        /// Updates an existing tour and its related entities.
        /// </summary>
        /// <param name="id">The ID of the tour to update</param>
        /// <param name="tourDto">The tour update data transfer object</param>
        /// <param name="imageFiles">Optional collection of new image files to upload</param>
        /// <returns>The updated Tour entity</returns>
        Task<Tour> UpdateTourAsync(Guid id, TourUpdateDto tourDto, IFormFileCollection? imageFiles);

        /// <summary>
        /// Performs a soft delete on a tour.
        /// </summary>
        /// <param name="id">The ID of the tour to delete</param>
        /// <returns>True if deletion was successful</returns>
        Task<bool> DeleteTourAsync(Guid id);

        /// <summary>
        /// Gets detailed information about a tour including all related entities.
        /// </summary>
        /// <param name="id">The ID of the tour</param>
        /// <returns>The Tour entity with all related data, or null if not found</returns>
        Task<Tour?> GetTourDetailsAsync(Guid id);

        /// <summary>
        /// Gets all non-deleted tours.
        /// </summary>
        /// <returns>Collection of Tour entities</returns>
        Task<IEnumerable<Tour>> GetAllToursAsync();

        /// <summary>
        /// Checks if a tour exists.
        /// </summary>
        /// <param name="id">The ID of the tour</param>
        /// <returns>True if the tour exists</returns>
        Task<bool> TourExistsAsync(Guid id);
    }
}
