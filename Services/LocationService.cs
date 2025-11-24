using Microsoft.EntityFrameworkCore;
using TourViet.Data;
using TourViet.Models;
using TourViet.Services.Interfaces;

namespace TourViet.Services
{
    /// <summary>
    /// Service for handling location and country operations.
    /// </summary>
    public class LocationService : ILocationService
    {
        private readonly TourBookingDbContext _context;

        public LocationService(TourBookingDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public async Task<Location> CreateOrUpdateLocationAsync(LocationDto locationDto, Guid? existingLocationId = null)
        {
            Location location;

            if (existingLocationId.HasValue)
            {
                // Update existing location
                location = await _context.Locations.FindAsync(existingLocationId.Value)
                    ?? throw new InvalidOperationException($"Location with ID {existingLocationId} not found");

                location.LocationName = locationDto.LocationName!;
                location.Address = locationDto.Address;
                location.City = locationDto.City;
                location.Latitude = locationDto.Latitude;
                location.Longitude = locationDto.Longitude;
                location.Description = locationDto.Description;
                location.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Create new location
                location = new Location
                {
                    LocationName = locationDto.LocationName!,
                    Address = locationDto.Address,
                    City = locationDto.City,
                    Latitude = locationDto.Latitude,
                    Longitude = locationDto.Longitude,
                    Description = locationDto.Description,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Locations.Add(location);
            }

            // Handle country if provided
            if (!string.IsNullOrWhiteSpace(locationDto.CountryName))
            {
                var country = await EnsureCountryExistsAsync(locationDto.CountryName);
                location.CountryID = country.CountryID;
            }

            await _context.SaveChangesAsync();
            return location;
        }

        /// <inheritdoc/>
        public async Task<Country> EnsureCountryExistsAsync(string countryName)
        {
            var country = await _context.Countries
                .FirstOrDefaultAsync(c => c.CountryName == countryName);

            if (country == null)
            {
                // Generate unique ISO2 code
                var iso2 = await GenerateUniqueISO2Async(countryName);

                country = new Country
                {
                    CountryName = countryName,
                    ISO2 = iso2,
                    ISO3 = null,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Countries.Add(country);
                await _context.SaveChangesAsync();
            }

            return country;
        }

        /// <inheritdoc/>
        public async Task<string> GenerateUniqueISO2Async(string countryName)
        {
            // Generate base ISO2 from first 2 characters of country name
            var baseIso2 = countryName.Length >= 2
                ? countryName.Substring(0, 2).ToUpper()
                : countryName.ToUpper().PadRight(2, 'X');

            var iso2 = baseIso2;
            var counter = 1;

            // Ensure uniqueness
            while (await _context.Countries.AnyAsync(c => c.ISO2 == iso2))
            {
                iso2 = baseIso2.Substring(0, 1) + counter.ToString();
                counter++;
            }

            return iso2;
        }
    }
}
