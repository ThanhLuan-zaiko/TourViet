using TourViet.Models;
using TourViet.Models.DTOs;

namespace TourViet.Services.Interfaces
{
    /// <summary>
    /// Service interface for location and country management operations.
    /// </summary>
    public interface ILocationService
    {
        /// <summary>
        /// Creates or updates a location based on the provided DTO.
        /// </summary>
        /// <param name="locationDto">The location data transfer object</param>
        /// <param name="existingLocationId">Optional existing location ID to update</param>
        /// <returns>The created or updated Location entity</returns>
        Task<Location> CreateOrUpdateLocationAsync(LocationDto locationDto, Guid? existingLocationId = null);

        /// <summary>
        /// Ensures a country exists in the database, creating it if necessary.
        /// Generates unique ISO2 code if not provided.
        /// </summary>
        /// <param name="countryName">The name of the country</param>
        /// <returns>The Country entity</returns>
        Task<Country> EnsureCountryExistsAsync(string countryName);

        /// <summary>
        /// Generates a unique ISO2 code for a country name.
        /// </summary>
        /// <param name="countryName">The name of the country</param>
        /// <returns>A unique ISO2 code</returns>
        Task<string> GenerateUniqueISO2Async(string countryName);
    }

    /// <summary>
    /// DTO for location data from the form.
    /// </summary>
    public class LocationDto
    {
        public string? LocationName { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? Description { get; set; }
        public string? CountryName { get; set; }
    }
}
