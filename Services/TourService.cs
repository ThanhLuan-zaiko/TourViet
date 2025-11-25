using Microsoft.EntityFrameworkCore;
using TourViet.Data;
using TourViet.Models;
using TourViet.Models.DTOs;
using TourViet.Services.Interfaces;

namespace TourViet.Services
{
    /// <summary>
    /// Service for handling tour business logic operations.
    /// </summary>
    public class TourService : ITourService
    {
        private readonly TourBookingDbContext _context;
        private readonly IImageService _imageService;
        private readonly ILocationService _locationService;

        public TourService(
            TourBookingDbContext context,
            IImageService imageService,
            ILocationService locationService)
        {
            _context = context;
            _imageService = imageService;
            _locationService = locationService;
        }

        /// <inheritdoc/>
        public async Task<Tour> CreateTourAsync(TourCreateDto tourDto, IFormFileCollection? imageFiles)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Create or get location
                Location? location = null;
                if (!string.IsNullOrWhiteSpace(tourDto.NewLocationName))
                {
                    var locationDto = new LocationDto
                    {
                        LocationName = tourDto.NewLocationName,
                        City = tourDto.NewLocationCity,
                        Address = tourDto.NewLocationAddress,
                        Latitude = tourDto.NewLocationLatitude,
                        Longitude = tourDto.NewLocationLongitude,
                        Description = tourDto.NewLocationDescription,
                        CountryName = tourDto.NewCountryName
                    };

                    location = await _locationService.CreateOrUpdateLocationAsync(locationDto);
                }

                // Create tour
                var tour = new Tour
                {
                    TourName = tourDto.TourName!,
                    Slug = tourDto.Slug,
                    ShortDescription = tourDto.ShortDescription,
                    Description = tourDto.Description,
                    CategoryID = tourDto.CategoryID,
                    LocationID = location?.LocationID ?? tourDto.LocationID,
                    DefaultGuideID = tourDto.DefaultGuideID,
                    IsPublished = tourDto.IsPublished,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Tours.Add(tour);
                await _context.SaveChangesAsync();

                // Add services
                if (tourDto.TourServiceIds != null && tourDto.TourServiceIds.Any())
                {
                    foreach (var serviceId in tourDto.TourServiceIds)
                    {
                        _context.TourServices.Add(new Models.TourService
                        {
                            TourID = tour.TourID,
                            ServiceID = serviceId,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                // Process images
                if (imageFiles != null && imageFiles.Count > 0)
                {
                    var tourImages = await _imageService.ProcessMultipleImagesAsync(tour.TourID, imageFiles);
                    foreach (var tourImage in tourImages)
                    {
                        _context.TourImages.Add(tourImage);
                    }
                }

                // Add itineraries
                if (tourDto.Itineraries != null)
                {
                    foreach (var itinerary in tourDto.Itineraries)
                    {
                        itinerary.TourID = tour.TourID;
                        _context.Itineraries.Add(itinerary);
                    }
                }

                // Add prices
                if (tourDto.TourPrices != null)
                {
                    foreach (var price in tourDto.TourPrices)
                    {
                        price.TourID = tour.TourID;
                        _context.TourPrices.Add(price);
                    }
                }

                // Add instances
                if (tourDto.TourInstances != null)
                {
                    foreach (var instance in tourDto.TourInstances)
                    {
                        instance.TourID = tour.TourID;
                        instance.CreatedAt = DateTime.UtcNow;
                        _context.TourInstances.Add(instance);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return tour;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Tour> UpdateTourAsync(Guid id, TourUpdateDto tourDto, IFormFileCollection? imageFiles)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Get existing tour with AsNoTracking to avoid conflicts
                var existingTour = await _context.Tours
                    .AsNoTracking()
                    .Include(t => t.Location)
                    .Include(t => t.TourServices)
                    .Include(t => t.TourImages)
                    .Include(t => t.Itineraries)
                    .Include(t => t.TourPrices)
                    .Include(t => t.TourInstances)
                    .FirstOrDefaultAsync(t => t.TourID == id)
                    ?? throw new InvalidOperationException($"Tour with ID {id} not found");

                // Attach and mark Tour as modified
                _context.Tours.Attach(existingTour);
                _context.Entry(existingTour).State = EntityState.Modified;

                // Detach all DTO entities to prevent tracking conflicts
                DetachDtoEntities(tourDto);

                // Update basic tour properties
                existingTour.TourName = tourDto.TourName!;
                existingTour.Slug = tourDto.Slug;
                existingTour.ShortDescription = tourDto.ShortDescription;
                existingTour.Description = tourDto.Description;
                existingTour.CategoryID = tourDto.CategoryID;
                existingTour.DefaultGuideID = tourDto.DefaultGuideID;
                existingTour.IsPublished = tourDto.IsPublished;
                existingTour.UpdatedAt = DateTime.UtcNow;

                // Update location if provided
                if (!string.IsNullOrWhiteSpace(tourDto.NewLocationName))
                {
                    var locationDto = new LocationDto
                    {
                        LocationName = tourDto.NewLocationName,
                        City = tourDto.NewLocationCity,
                        Address = tourDto.NewLocationAddress,
                        Latitude = tourDto.NewLocationLatitude,
                        Longitude = tourDto.NewLocationLongitude,
                        Description = tourDto.NewLocationDescription,
                        CountryName = tourDto.NewCountryName
                    };

                    var location = await _locationService.CreateOrUpdateLocationAsync(
                        locationDto,
                        existingTour.LocationID
                    );
                    existingTour.LocationID = location.LocationID;
                }

                // Update TourServices - remove old and add new
                var existingServices = await _context.TourServices
                    .Where(ts => ts.TourID == id)
                    .ToListAsync();
                _context.TourServices.RemoveRange(existingServices);

                if (tourDto.TourServiceIds != null && tourDto.TourServiceIds.Any())
                {
                    foreach (var serviceId in tourDto.TourServiceIds)
                    {
                        _context.TourServices.Add(new Models.TourService
                        {
                            TourID = id,
                            ServiceID = serviceId,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                // Handle Images
                if (imageFiles != null && imageFiles.Count > 0)
                {
                    var currentMaxSort = await _imageService.GetMaxSortOrderAsync(id);
                    var tourImages = await _imageService.ProcessMultipleImagesAsync(
                        id,
                        imageFiles,
                        currentMaxSort + 1
                    );

                    foreach (var tourImage in tourImages)
                    {
                        _context.TourImages.Add(tourImage);
                    }
                }

                // Update Itineraries
                await UpdateItinerariesAsync(id, tourDto.Itineraries);

                // Update TourPrices
                await UpdateTourPricesAsync(id, tourDto.TourPrices);

                // Update TourInstances
                await UpdateTourInstancesAsync(id, tourDto.TourInstances);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return existingTour;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteTourAsync(Guid id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                var tour = await _context.Tours
                    .Include(t => t.TourImages)
                    .Include(t => t.Itineraries)
                    .Include(t => t.TourPrices)
                    .Include(t => t.TourInstances)
                    .FirstOrDefaultAsync(t => t.TourID == id);
                
                if (tour == null)
                    return false;

                // Delete all related images from database (ImageService will handle physical files)
                // Use ToList() to avoid "Collection was modified" exception
                foreach (var image in tour.TourImages.ToList())
                {
                    await _imageService.DeleteImageAsync(image.ImageID);
                }

                // Delete the entire tour image folder
                var tourFolderPath = Path.Combine("Uploads", id.ToString());
                if (Directory.Exists(tourFolderPath))
                {
                    try
                    {
                        Directory.Delete(tourFolderPath, recursive: true);
                        Console.WriteLine($"Successfully deleted tour folder: {tourFolderPath}");
                    }
                    catch (Exception ex)
                    {
                        // Log error but don't fail the transaction
                        Console.WriteLine($"Warning: Could not delete tour folder {tourFolderPath}: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"Tour folder not found: {tourFolderPath}");
                }

                // Hard delete itineraries
                _context.Itineraries.RemoveRange(tour.Itineraries);

                // Hard delete tour prices
                _context.TourPrices.RemoveRange(tour.TourPrices);

                // Hard delete tour instances
                _context.TourInstances.RemoveRange(tour.TourInstances);

                // Hard delete tour services relationship
                var tourServices = await _context.Set<Models.TourService>()
                    .Where(ts => ts.TourID == id)
                    .ToListAsync();
                _context.Set<Models.TourService>().RemoveRange(tourServices);

                // Hard delete the tour itself
                _context.Tours.Remove(tour);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Tour?> GetTourDetailsAsync(Guid id)
        {
            return await _context.Tours
                .Include(t => t.Location)
                    .ThenInclude(l => l!.Country)
                .Include(t => t.Category)
                .Include(t => t.TourServices)
                    .ThenInclude(ts => ts.Service)
                .Include(t => t.TourImages)
                .Include(t => t.Itineraries)
                .Include(t => t.TourPrices)
                .Include(t => t.TourInstances)
                .FirstOrDefaultAsync(t => t.TourID == id);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Tour>> GetAllToursAsync()
        {
            return await _context.Tours
                .Where(t => !t.IsDeleted)
                .Include(t => t.Location)
                .Include(t => t.Category)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<bool> TourExistsAsync(Guid id)
        {
            return await _context.Tours.AnyAsync(t => t.TourID == id);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Tour>> GetPublishedToursAsync()
        {
            return await _context.Tours
                .Where(t => !t.IsDeleted && t.IsPublished)
                .Include(t => t.Location)
                .Include(t => t.Category)
                .Include(t => t.TourImages)
                .Include(t => t.TourPrices)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Tour>> GetPublishedToursPagedAsync(int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 6;

            return await _context.Tours
                .Where(t => !t.IsDeleted && t.IsPublished)
                .Include(t => t.Location)
                .Include(t => t.Category)
                .Include(t => t.TourImages)
                .Include(t => t.TourPrices)
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Tour>> GetTrendingToursPagedAsync(int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 6;

            return await _context.Tours
                .Where(t => !t.IsDeleted && t.IsPublished)
                .Where(t => t.TourInstances.Sum(ti => ti.Bookings.Count) >= 5)
                .Include(t => t.Location)
                .Include(t => t.Category)
                .Include(t => t.TourImages)
                .Include(t => t.TourPrices)
                .OrderByDescending(t => t.TourInstances.Sum(ti => ti.Bookings.Count))
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Tour>> GetDomesticToursPagedAsync(int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 6;

            return await _context.Tours
                .Where(t => !t.IsDeleted && t.IsPublished)
                .Where(t => t.Location != null && t.Location.Country != null && t.Location.Country.ISO2 == "VI")
                .Include(t => t.Location)
                    .ThenInclude(l => l!.Country)
                .Include(t => t.Category)
                .Include(t => t.TourImages)
                .Include(t => t.TourPrices)
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Tour>> GetInternationalToursPagedAsync(int page, int pageSize, Guid? countryId = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 6;

            var query = _context.Tours
                .Where(t => !t.IsDeleted && t.IsPublished)
                .Where(t => t.Location != null && t.Location.Country != null && t.Location.Country.ISO2 != "VI");

            // Apply country filter if specified
            if (countryId.HasValue)
            {
                query = query.Where(t => t.Location!.CountryID == countryId.Value);
            }

            return await query
                .Include(t => t.Location)
                    .ThenInclude(l => l!.Country)
                .Include(t => t.Category)
                .Include(t => t.TourImages)
                .Include(t => t.TourPrices)
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Country>> GetInternationalCountriesAsync()
        {
            return await _context.Tours
                .Where(t => !t.IsDeleted && t.IsPublished)
                .Where(t => t.Location != null && t.Location.Country != null && t.Location.Country.ISO2 != "VI")
                .Select(t => t.Location!.Country!)
                .Distinct()
                .OrderBy(c => c.CountryName)
                .ToListAsync();
        }

        #region Private Helper Methods

        private void DetachDtoEntities(TourUpdateDto tourDto)
        {
            if (tourDto.Itineraries != null)
            {
                foreach (var item in tourDto.Itineraries)
                {
                    var entry = _context.Entry(item);
                    if (entry.State != EntityState.Detached)
                    {
                        entry.State = EntityState.Detached;
                    }
                }
            }

            if (tourDto.TourPrices != null)
            {
                foreach (var item in tourDto.TourPrices)
                {
                    var entry = _context.Entry(item);
                    if (entry.State != EntityState.Detached)
                    {
                        entry.State = EntityState.Detached;
                    }
                }
            }

            if (tourDto.TourInstances != null)
            {
                foreach (var item in tourDto.TourInstances)
                {
                    var entry = _context.Entry(item);
                    if (entry.State != EntityState.Detached)
                    {
                        entry.State = EntityState.Detached;
                    }
                }
            }
        }

        private async Task UpdateItinerariesAsync(Guid tourId, IEnumerable<Itinerary>? itineraries)
        {
            if (itineraries == null) return;

            var idsToKeep = itineraries
                .Where(i => i.ItineraryID != Guid.Empty)
                .Select(i => i.ItineraryID)
                .ToList();

            var toDelete = await _context.Itineraries
                .Where(i => i.TourID == tourId && !idsToKeep.Contains(i.ItineraryID))
                .ToListAsync();
            _context.Itineraries.RemoveRange(toDelete);

            foreach (var itineraryDto in itineraries)
            {
                if (itineraryDto.ItineraryID != Guid.Empty)
                {
                    var existing = await _context.Itineraries.FindAsync(itineraryDto.ItineraryID);
                    if (existing != null)
                    {
                        existing.DayIndex = itineraryDto.DayIndex;
                        existing.Title = itineraryDto.Title;
                        existing.Description = itineraryDto.Description;
                    }
                }
                else
                {
                    var newItinerary = new Itinerary
                    {
                        ItineraryID = Guid.NewGuid(),
                        TourID = tourId,
                        DayIndex = itineraryDto.DayIndex,
                        Title = itineraryDto.Title,
                        Description = itineraryDto.Description
                    };
                    _context.Itineraries.Add(newItinerary);
                }
            }
        }

        private async Task UpdateTourPricesAsync(Guid tourId, IEnumerable<TourPrice>? prices)
        {
            if (prices == null) return;

            var idsToKeep = prices
                .Where(p => p.TourPriceID != Guid.Empty)
                .Select(p => p.TourPriceID)
                .ToList();

            var toDelete = await _context.TourPrices
                .Where(p => p.TourID == tourId && !idsToKeep.Contains(p.TourPriceID))
                .ToListAsync();
            _context.TourPrices.RemoveRange(toDelete);

            foreach (var priceDto in prices)
            {
                if (priceDto.TourPriceID != Guid.Empty)
                {
                    var existing = await _context.TourPrices.FindAsync(priceDto.TourPriceID);
                    if (existing != null)
                    {
                        existing.PriceType = priceDto.PriceType;
                        existing.Amount = priceDto.Amount;
                        existing.Currency = string.IsNullOrEmpty(priceDto.Currency) ? "USD" : priceDto.Currency;
                    }
                }
                else
                {
                    var newPrice = new TourPrice
                    {
                        TourPriceID = Guid.NewGuid(),
                        TourID = tourId,
                        PriceType = priceDto.PriceType,
                        Amount = priceDto.Amount,
                        Currency = string.IsNullOrEmpty(priceDto.Currency) ? "USD" : priceDto.Currency
                    };
                    _context.TourPrices.Add(newPrice);
                }
            }
        }

        private async Task UpdateTourInstancesAsync(Guid tourId, IEnumerable<TourInstance>? instances)
        {
            if (instances == null) return;

            var idsToKeep = instances
                .Where(i => i.InstanceID != Guid.Empty)
                .Select(i => i.InstanceID)
                .ToList();

            var toDelete = await _context.TourInstances
                .Where(i => i.TourID == tourId && !idsToKeep.Contains(i.InstanceID))
                .ToListAsync();
            _context.TourInstances.RemoveRange(toDelete);

            foreach (var instanceDto in instances)
            {
                if (instanceDto.InstanceID != Guid.Empty)
                {
                    var existing = await _context.TourInstances.FindAsync(instanceDto.InstanceID);
                    if (existing != null)
                    {
                        existing.StartDate = instanceDto.StartDate;
                        existing.EndDate = instanceDto.EndDate;
                        existing.Capacity = instanceDto.Capacity;
                        existing.SeatsBooked = instanceDto.SeatsBooked;
                        existing.SeatsHeld = instanceDto.SeatsHeld;
                        existing.Status = string.IsNullOrEmpty(instanceDto.Status) ? "Scheduled" : instanceDto.Status;
                        existing.PriceBase = instanceDto.PriceBase;
                        existing.Currency = string.IsNullOrEmpty(instanceDto.Currency) ? "VND" : instanceDto.Currency;
                        existing.GuideID = instanceDto.GuideID;
                        existing.UpdatedAt = DateTime.UtcNow;
                    }
                }
                else
                {
                    var newInstance = new TourInstance
                    {
                        InstanceID = Guid.NewGuid(),
                        TourID = tourId,
                        StartDate = instanceDto.StartDate,
                        EndDate = instanceDto.EndDate,
                        Capacity = instanceDto.Capacity,
                        SeatsBooked = instanceDto.SeatsBooked,
                        SeatsHeld = instanceDto.SeatsHeld,
                        Status = string.IsNullOrEmpty(instanceDto.Status) ? "Scheduled" : instanceDto.Status,
                        PriceBase = instanceDto.PriceBase,
                        Currency = string.IsNullOrEmpty(instanceDto.Currency) ? "VND" : instanceDto.Currency,
                        GuideID = instanceDto.GuideID,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.TourInstances.Add(newInstance);
                }
            }
        }

        #endregion
    }
}