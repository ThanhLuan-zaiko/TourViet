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

        /// <summary>
        /// Gets all published tours for public display.
        /// </summary>
        /// <returns>Collection of published Tour entities with related data</returns>
        Task<IEnumerable<Tour>> GetPublishedToursAsync();

        /// <summary>
        /// Gets a paged list of published tours for public display.
        /// </summary>
        /// <param name="page">The page number (1-based)</param>
        /// <param name="pageSize">The number of items per page</param>
        /// <returns>Collection of published Tour entities for the requested page</returns>
        Task<IEnumerable<Tour>> GetPublishedToursPagedAsync(int page, int pageSize);

        /// <summary>
        /// Gets a paged list of trending tours (booked >= 5 times).
        /// </summary>
        /// <param name="page">The page number (1-based)</param>
        /// <param name="pageSize">The number of items per page</param>
        /// <returns>Collection of trending Tour entities</returns>
        Task<IEnumerable<Tour>> GetTrendingToursPagedAsync(int page, int pageSize);

        /// <summary>
        /// Gets a paged list of domestic tours (Vietnam only).
        /// </summary>
        /// <param name="page">The page number (1-based)</param>
        /// <param name="pageSize">The number of items per page</param>
        /// <returns>Collection of domestic Tour entities</returns>
        Task<IEnumerable<Tour>> GetDomesticToursPagedAsync(int page, int pageSize);

        /// <summary>
        /// Gets a paged list of international tours (non-Vietnam).
        /// </summary>
        /// <param name="page">The page number (1-based)</param>
        /// <param name="pageSize">The number of items per page</param>
        /// <param name="countryId">Optional country ID to filter by specific country</param>
        /// <returns>Collection of international Tour entities</returns>
        Task<IEnumerable<Tour>> GetInternationalToursPagedAsync(int page, int pageSize, Guid? countryId = null);

        /// <summary>
        /// Gets a list of countries that have international tours.
        /// </summary>
        /// <returns>Collection of Country entities (excluding Vietnam)</returns>
        Task<IEnumerable<Country>> GetInternationalCountriesAsync();

        /// <summary>
        /// Gets tour instances within a specific date range.
        /// </summary>
        /// <param name="startDate">Start date of the range</param>
        /// <param name="endDate">End date of the range</param>
        /// <returns>Collection of TourInstance entities</returns>
        Task<IEnumerable<TourInstance>> GetTourInstancesAsync(DateTime startDate, DateTime endDate);
    }
}
