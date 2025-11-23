using Microsoft.AspNetCore.Mvc;
using TourViet.Data;
using TourViet.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using TourViet.Models.DTOs;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace TourViet.Controllers
{
    public class TourController : Controller
    {
        private readonly TourBookingDbContext _context;

        public TourController(TourBookingDbContext context)
        {
            _context = context;
        }

        // GET: Tour/Create
        public async Task<IActionResult> Create()
        {
            // Lấy danh sách locations, categories, guides, countries và services từ database
            var locations = await _context.Locations
                .Include(l => l.Country)
                .ToListAsync();
            
            var categories = await _context.Categories.ToListAsync();
            var guides = await _context.Users.ToListAsync();
            var countries = await _context.Countries.ToListAsync();
            var services = await _context.Services.ToListAsync();

            ViewBag.Locations = locations;
            ViewBag.Categories = categories;
            ViewBag.Guides = guides;
            ViewBag.Countries = countries;
            ViewBag.Services = services;

            return View(new Tour());
        }

        // POST: Tour/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Tour tour, string? NewLocationName, string? NewLocationCity, 
            string? NewLocationAddress, decimal? NewLocationLatitude, decimal? NewLocationLongitude, 
            string? NewLocationDescription, string? NewCountryName, Guid? CountryID, List<Guid> TourServiceIds, List<IFormFile> files)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Nếu có tên quốc gia mới, tìm hoặc tạo Country
                if (!string.IsNullOrEmpty(NewCountryName) && !CountryID.HasValue)
                {
                    // Tìm country theo tên
                    var existingCountry = await _context.Countries
                        .FirstOrDefaultAsync(c => c.CountryName == NewCountryName);
                    
                    if (existingCountry != null)
                    {
                        CountryID = existingCountry.CountryID;
                    }
                    else
                    {
                        // Tạo country mới
                        var newCountry = new Country
                        {
                            CountryName = NewCountryName,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.Countries.Add(newCountry);
                        await _context.SaveChangesAsync();
                        CountryID = newCountry.CountryID;
                    }
                }

                // Nếu có thông tin location mới được nhập, tạo location mới
                if (!string.IsNullOrEmpty(NewLocationName))
                {
                    var newLocation = new Location
                    {
                        LocationName = NewLocationName,
                        City = NewLocationCity,
                        Address = NewLocationAddress,
                        Latitude = NewLocationLatitude,
                        Longitude = NewLocationLongitude,
                        Description = NewLocationDescription,
                        CountryID = CountryID,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Locations.Add(newLocation);
                    await _context.SaveChangesAsync();
                    tour.LocationID = newLocation.LocationID;
                }

                // Xử lý Services
                if (TourServiceIds != null && TourServiceIds.Any())
                {
                    foreach (var serviceId in TourServiceIds)
                    {
                        var tourService = new TourService
                        {
                            TourID = tour.TourID,
                            ServiceID = serviceId,
                            IsIncluded = true,
                            CreatedAt = DateTime.UtcNow
                        };
                        tour.TourServices.Add(tourService);
                    }
                }

                // Xử lý Images
                if (files != null && files.Count > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
                    var tourFolder = Path.Combine(uploadsFolder, tour.TourID.ToString());
                    
                    if (!Directory.Exists(tourFolder))
                    {
                        Directory.CreateDirectory(tourFolder);
                    }

                    int sortOrder = 0;
                    foreach (var file in files)
                    {
                        if (file.Length > 0)
                        {
                            var fileName = Path.GetFileNameWithoutExtension(file.FileName);
                            var newFileName = $"{Guid.NewGuid()}_{fileName}.webp";
                            var filePath = Path.Combine(tourFolder, newFileName);

                            using (var stream = file.OpenReadStream())
                            using (var image = await SixLabors.ImageSharp.Image.LoadAsync(stream))
                            {
                                await image.SaveAsWebpAsync(filePath);
                            }

                            var tourImage = new TourImage
                            {
                                TourID = tour.TourID,
                                Provider = "Local",
                                Url = $"/Uploads/{tour.TourID}/{newFileName}",
                                Path = filePath,
                                FileName = newFileName,
                                MimeType = "image/webp",
                                FileSize = (int)new FileInfo(filePath).Length,
                                IsPrimary = sortOrder == 0,
                                SortOrder = sortOrder++,
                                UploadedAt = DateTime.UtcNow,
                                IsPublic = true
                            };
                            tour.TourImages.Add(tourImage);
                        }
                    }
                }

                // Xử lý các thuộc tính mặc định và gán ID
                tour.CreatedAt = DateTime.UtcNow;
                tour.IsDeleted = false;

                foreach (var instance in tour.TourInstances)
                {
                    if (instance.InstanceID == Guid.Empty) instance.InstanceID = Guid.NewGuid();
                    instance.TourID = tour.TourID;
                    instance.CreatedAt = DateTime.UtcNow;
                    if (string.IsNullOrEmpty(instance.Currency)) instance.Currency = "USD";
                    if (string.IsNullOrEmpty(instance.Status)) instance.Status = "Open";
                    
                    // Xóa lỗi validation cho TourID vì chúng ta vừa gán nó
                    ModelState.Remove($"TourInstances[{tour.TourInstances.ToList().IndexOf(instance)}].TourID");
                    ModelState.Remove($"TourInstances[{tour.TourInstances.ToList().IndexOf(instance)}].Tour");
                }

                foreach (var price in tour.TourPrices)
                {
                    if (price.TourPriceID == Guid.Empty) price.TourPriceID = Guid.NewGuid();
                    price.TourID = tour.TourID;
                    if (string.IsNullOrEmpty(price.Currency)) price.Currency = "USD";
                    
                    ModelState.Remove($"TourPrices[{tour.TourPrices.ToList().IndexOf(price)}].TourID");
                    ModelState.Remove($"TourPrices[{tour.TourPrices.ToList().IndexOf(price)}].Tour");
                }

                foreach (var itinerary in tour.Itineraries)
                {
                    if (itinerary.ItineraryID == Guid.Empty) itinerary.ItineraryID = Guid.NewGuid();
                    itinerary.TourID = tour.TourID;
                    
                    ModelState.Remove($"Itineraries[{tour.Itineraries.ToList().IndexOf(itinerary)}].TourID");
                    ModelState.Remove($"Itineraries[{tour.Itineraries.ToList().IndexOf(itinerary)}].Tour");
                }

                // Xóa các lỗi validation chung liên quan đến navigation properties bắt buộc mà EF Core sẽ tự xử lý
                foreach (var key in ModelState.Keys.ToList())
                {
                    if (key.Contains(".TourID") || key.Contains(".Tour"))
                    {
                        ModelState.Remove(key);
                    }
                }

                if (ModelState.IsValid)
                {
                    try
                    {
                        _context.Add(tour);
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        return RedirectToAction("ManageTours", "Home");
                    }
                    catch (DbUpdateException ex)
                    {
                        await transaction.RollbackAsync();
                        
                        // Log chi tiết lỗi
                        var innerMessage = ex.InnerException?.Message ?? ex.Message;
                        Console.WriteLine($"Database Error: {innerMessage}");
                        
                        // Trả về view với thông báo lỗi chi tiết
                        ModelState.AddModelError(string.Empty, $"Lỗi lưu dữ liệu: {innerMessage}");
                        
                        // Load lại dữ liệu cho view
                        var locations = await _context.Locations.ToListAsync();
                        var categories = await _context.Categories.ToListAsync();
                        var guides = await _context.Users.ToListAsync();
                        var countries = await _context.Countries.ToListAsync();
                        var services = await _context.Services.ToListAsync();

                        ViewBag.Locations = locations;
                        ViewBag.Categories = categories;
                        ViewBag.Guides = guides;
                        ViewBag.Countries = countries;
                        ViewBag.Services = services;

                        return View(tour);
                    }
                }
                else
                {
                    // Rollback nếu validation failed
                    await transaction.RollbackAsync();
                    
                    // Log các lỗi validation
                    Console.WriteLine("ModelState Errors:");
                    foreach (var state in ModelState)
                    {
                        foreach (var error in state.Value.Errors)
                        {
                            Console.WriteLine($"{state.Key}: {error.ErrorMessage}");
                        }
                    }
                    
                    // Load lại dữ liệu cho view
                    var locationsError = await _context.Locations.ToListAsync();
                    var categoriesError = await _context.Categories.ToListAsync();
                    var guidesError = await _context.Users.ToListAsync();
                    var countriesError = await _context.Countries.ToListAsync();
                    var servicesError = await _context.Services.ToListAsync();

                    ViewBag.Locations = locationsError;
                    ViewBag.Categories = categoriesError;
                    ViewBag.Guides = guidesError;
                    ViewBag.Countries = countriesError;
                    ViewBag.Services = servicesError;

                    return View(tour);
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"General Error: {ex.Message}");
                Console.WriteLine($"Inner Exception: {ex.InnerException?.Message}");
                throw;
            }
        }

        // GET: Tour/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tour = await _context.Tours
                .Include(t => t.Location)
                    .ThenInclude(l => l!.Country)
                .Include(t => t.Category)
                .Include(t => t.DefaultGuide)
                .Include(t => t.Itineraries.OrderBy(i => i.DayIndex))
                .Include(t => t.TourPrices)
                .Include(t => t.TourInstances)
                    .ThenInclude(ti => ti.Guide)
                .Include(t => t.TourServices)
                    .ThenInclude(ts => ts.Service)
                .Include(t => t.TourImages.OrderBy(img => img.SortOrder))
                .FirstOrDefaultAsync(m => m.TourID == id);
            
            if (tour == null)
            {
                return NotFound();
            }

            return View(tour);
        }

        // GET: Tour/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tour = await _context.Tours
                .Include(t => t.Itineraries)
                .Include(t => t.TourPrices)
                .Include(t => t.TourInstances)
                .Include(t => t.TourServices)
                .Include(t => t.TourImages)
                .Include(t => t.Location)
                .FirstOrDefaultAsync(m => m.TourID == id);
            
            if (tour == null)
            {
                return NotFound();
            }

            var locations = await _context.Locations.ToListAsync();
            var categories = await _context.Categories.ToListAsync();
            var guides = await _context.Users.ToListAsync();
            var countries = await _context.Countries.ToListAsync();
            var services = await _context.Services.ToListAsync();

            ViewBag.Locations = locations;
            ViewBag.Categories = categories;
            ViewBag.Guides = guides;
            ViewBag.Countries = countries;
            ViewBag.Services = services;

            return View(tour);
        }

        // POST: Tour/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [FromForm] TourUpdateDto tourDto)
        {
            if (id != tourDto.TourID)
            {
                return NotFound();
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Get existing tour WITHOUT tracking to avoid conflicts
                var existingTour = await _context.Tours
                    .AsNoTracking()
                    .Include(t => t.Location)
                    .Include(t => t.TourServices)
                    .Include(t => t.TourImages)
                    .Include(t => t.Itineraries)
                    .Include(t => t.TourPrices)
                    .Include(t => t.TourInstances)
                    .FirstOrDefaultAsync(t => t.TourID == id);

                if (existingTour == null)
                {
                    await transaction.RollbackAsync();
                    return NotFound();
                }

                // Now attach and track only the Tour entity
                _context.Tours.Attach(existingTour);
                _context.Entry(existingTour).State = Microsoft.EntityFrameworkCore.EntityState.Modified;


                // CRITICAL: Detach all entities from DTO to prevent tracking conflicts
                // Model binding creates new entity instances that EF Core might try to track
                if (tourDto.Itineraries != null)
                {
                    foreach (var item in tourDto.Itineraries)
                    {
                        var entry = _context.Entry(item);
                        if (entry.State != Microsoft.EntityFrameworkCore.EntityState.Detached)
                        {
                            entry.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                        }
                    }
                }
                
                if (tourDto.TourPrices != null)
                {
                    foreach (var item in tourDto.TourPrices)
                    {
                        var entry = _context.Entry(item);
                        if (entry.State != Microsoft.EntityFrameworkCore.EntityState.Detached)
                        {
                            entry.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                        }
                    }
                }
                
                if (tourDto.TourInstances != null)
                {
                    foreach (var item in tourDto.TourInstances)
                    {
                        var entry = _context.Entry(item);
                        if (entry.State != Microsoft.EntityFrameworkCore.EntityState.Detached)
                        {
                            entry.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                        }
                    }
                }


                // Update basic tour properties
                existingTour.TourName = tourDto.TourName!;
                existingTour.Slug = tourDto.Slug;
                existingTour.ShortDescription = tourDto.ShortDescription;
                existingTour.Description = tourDto.Description;
                existingTour.CategoryID = tourDto.CategoryID;
                existingTour.DefaultGuideID = tourDto.DefaultGuideID;
                existingTour.IsPublished = tourDto.IsPublished;
                existingTour.UpdatedAt = DateTime.UtcNow;

                // Handle Location
                if (!string.IsNullOrWhiteSpace(tourDto.NewLocationName) && tourDto.NewLocationLatitude.HasValue && tourDto.NewLocationLongitude.HasValue)
                {
                    // Check if we should update existing location or create new one
                    if (existingTour.LocationID.HasValue && existingTour.Location != null)
                    {
                        // Update existing location
                        existingTour.Location.LocationName = tourDto.NewLocationName;
                        existingTour.Location.City = tourDto.NewLocationCity;
                        existingTour.Location.Address = tourDto.NewLocationAddress;
                        existingTour.Location.Latitude = tourDto.NewLocationLatitude;
                        existingTour.Location.Longitude = tourDto.NewLocationLongitude;
                        existingTour.Location.Description = tourDto.NewLocationDescription;
                        existingTour.Location.UpdatedAt = DateTime.UtcNow;

                        // Update country if provided
                        if (!string.IsNullOrWhiteSpace(tourDto.NewCountryName))
                        {
                            var country = await _context.Countries.FirstOrDefaultAsync(c => c.CountryName == tourDto.NewCountryName);
                            if (country == null)
                            {
                                // Generate a unique ISO2 from country name (first 2 uppercase chars + number if needed)
                                var baseIso2 = tourDto.NewCountryName.Length >= 2 
                                    ? tourDto.NewCountryName.Substring(0, 2).ToUpper() 
                                    : tourDto.NewCountryName.ToUpper().PadRight(2, 'X');
                                
                                var iso2 = baseIso2;
                                var counter = 1;
                                while (await _context.Countries.AnyAsync(c => c.ISO2 == iso2))
                                {
                                    iso2 = baseIso2.Substring(0, 1) + counter.ToString();
                                    counter++;
                                }

                                country = new Country { 
                                    CountryName = tourDto.NewCountryName,
                                    ISO2 = iso2,
                                    ISO3 = null
                                };
                                _context.Countries.Add(country);
                                await _context.SaveChangesAsync();
                            }
                            existingTour.Location.CountryID = country.CountryID;
                        }
                    }
                    else
                    {
                        // Create new location
                        var location = new Location
                        {
                            LocationName = tourDto.NewLocationName,
                            City = tourDto.NewLocationCity,
                            Address = tourDto.NewLocationAddress,
                            Latitude = tourDto.NewLocationLatitude,
                            Longitude = tourDto.NewLocationLongitude,
                            Description = tourDto.NewLocationDescription
                        };

                        if (!string.IsNullOrWhiteSpace(tourDto.NewCountryName))
                        {
                            var country = await _context.Countries.FirstOrDefaultAsync(c => c.CountryName == tourDto.NewCountryName);
                            if (country == null)
                            {
                                // Generate a unique ISO2 from country name
                                var baseIso2 = tourDto.NewCountryName.Length >= 2 
                                    ? tourDto.NewCountryName.Substring(0, 2).ToUpper() 
                                    : tourDto.NewCountryName.ToUpper().PadRight(2, 'X');
                                
                                var iso2 = baseIso2;
                                var counter = 1;
                                while (await _context.Countries.AnyAsync(c => c.ISO2 == iso2))
                                {
                                    iso2 = baseIso2.Substring(0, 1) + counter.ToString();
                                    counter++;
                                }

                                country = new Country { 
                                    CountryName = tourDto.NewCountryName,
                                    ISO2 = iso2,
                                    ISO3 = null
                                };
                                _context.Countries.Add(country);
                                await _context.SaveChangesAsync();
                            }
                            location.CountryID = country.CountryID;
                        }

                        _context.Locations.Add(location);
                        await _context.SaveChangesAsync();
                        existingTour.LocationID = location.LocationID;
                    }
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
                        _context.TourServices.Add(new TourService
                        {
                            TourID = id,
                            ServiceID = serviceId
                        });
                    }
                }

                // Handle Images
                if (tourDto.TourImageFiles != null && tourDto.TourImageFiles.Any())
                {
                    var uploadDir = Path.Combine("Uploads", id.ToString());
                    if (!Directory.Exists(uploadDir))
                    {
                        Directory.CreateDirectory(uploadDir);
                    }

                    // Get current max sort order
                    var currentMaxSort = await _context.TourImages
                        .Where(ti => ti.TourID == id)
                        .Select(ti => (int?)ti.SortOrder)
                        .MaxAsync() ?? 0;

                    foreach (var file in tourDto.TourImageFiles)
                    {
                        if (file.Length > 0)
                        {
                            var fileName = $"{Guid.NewGuid()}.webp";
                            var filePath = Path.Combine(uploadDir, fileName);

                            using (var image = await Image.LoadAsync(file.OpenReadStream()))
                            {
                                await image.SaveAsWebpAsync(filePath);
                            }

                            _context.TourImages.Add(new TourImage
                            {
                                TourID = id,
                                Url = $"/Uploads/{id}/{fileName}",
                                MimeType = "image/webp",
                                SortOrder = ++currentMaxSort
                            });
                        }
                    }
                }

                // Update Itineraries
                // Delete removed items
                var itineraryIdsToKeep = tourDto.Itineraries?.Where(i => i.ItineraryID != Guid.Empty).Select(i => i.ItineraryID).ToList() ?? new List<Guid>();
                var itinerariesToDelete = await _context.Itineraries
                    .Where(i => i.TourID == id && !itineraryIdsToKeep.Contains(i.ItineraryID))
                    .ToListAsync();
                _context.Itineraries.RemoveRange(itinerariesToDelete);

                // Update existing and Add new
                if (tourDto.Itineraries != null)
                {
                    foreach (var itineraryDto in tourDto.Itineraries)
                    {
                        if (itineraryDto.ItineraryID != Guid.Empty)
                        {
                            // Update existing - fetch from DB and update
                            var existingItinerary = await _context.Itineraries.FindAsync(itineraryDto.ItineraryID);
                            if (existingItinerary != null)
                            {
                                existingItinerary.DayIndex = itineraryDto.DayIndex;
                                existingItinerary.Title = itineraryDto.Title;
                                existingItinerary.Description = itineraryDto.Description;
                            }
                        }
                        else
                        {
                            // Add new
                            var newItinerary = new Itinerary
                            {
                                ItineraryID = Guid.NewGuid(),
                                TourID = id,
                                DayIndex = itineraryDto.DayIndex,
                                Title = itineraryDto.Title,
                                Description = itineraryDto.Description
                            };
                            _context.Itineraries.Add(newItinerary);
                        }
                    }
                }

                // Update TourPrices
                var priceIdsToKeep = tourDto.TourPrices?.Where(p => p.TourPriceID != Guid.Empty).Select(p => p.TourPriceID).ToList() ?? new List<Guid>();
                var pricesToDelete = await _context.TourPrices
                    .Where(p => p.TourID == id && !priceIdsToKeep.Contains(p.TourPriceID))
                    .ToListAsync();
                _context.TourPrices.RemoveRange(pricesToDelete);

                if (tourDto.TourPrices != null)
                {
                    foreach (var priceDto in tourDto.TourPrices)
                    {
                        if (priceDto.TourPriceID != Guid.Empty)
                        {
                            var existingPrice = await _context.TourPrices.FindAsync(priceDto.TourPriceID);
                            if (existingPrice != null)
                            {
                                existingPrice.PriceType = priceDto.PriceType;
                                existingPrice.Amount = priceDto.Amount;
                                existingPrice.Currency = string.IsNullOrEmpty(priceDto.Currency) ? "USD" : priceDto.Currency;
                            }
                        }
                        else
                        {
                            // Add new
                            var newPrice = new TourPrice
                            {
                                TourPriceID = Guid.NewGuid(),
                                TourID = id,
                                PriceType = priceDto.PriceType,
                                Amount = priceDto.Amount,
                                Currency = string.IsNullOrEmpty(priceDto.Currency) ? "USD" : priceDto.Currency
                            };
                            _context.TourPrices.Add(newPrice);
                        }
                    }
                }

                // Update TourInstances
                var instanceIdsToKeep = tourDto.TourInstances?.Where(i => i.InstanceID != Guid.Empty).Select(i => i.InstanceID).ToList() ?? new List<Guid>();
                var instancesToDelete = await _context.TourInstances
                    .Where(i => i.TourID == id && !instanceIdsToKeep.Contains(i.InstanceID))
                    .ToListAsync();
                _context.TourInstances.RemoveRange(instancesToDelete);

                if (tourDto.TourInstances != null)
                {
                    foreach (var instanceDto in tourDto.TourInstances)
                    {
                        if (instanceDto.InstanceID != Guid.Empty)
                        {
                            var existingInstance = await _context.TourInstances.FindAsync(instanceDto.InstanceID);
                            if (existingInstance != null)
                            {
                                existingInstance.StartDate = instanceDto.StartDate;
                                existingInstance.EndDate = instanceDto.EndDate;
                                existingInstance.Capacity = instanceDto.Capacity;
                                existingInstance.SeatsBooked = instanceDto.SeatsBooked;
                                existingInstance.SeatsHeld = instanceDto.SeatsHeld;
                                existingInstance.Status = string.IsNullOrEmpty(instanceDto.Status) ? "Scheduled" : instanceDto.Status;
                                existingInstance.PriceBase = instanceDto.PriceBase;
                                existingInstance.Currency = string.IsNullOrEmpty(instanceDto.Currency) ? "VND" : instanceDto.Currency;
                                existingInstance.GuideID = instanceDto.GuideID;
                                existingInstance.UpdatedAt = DateTime.UtcNow;
                            }
                        }
                        else
                        {
                            // Add new
                            var newInstance = new TourInstance
                            {
                                InstanceID = Guid.NewGuid(),
                                TourID = id,
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

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return RedirectToAction("ManageTours", "Home");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TourExists(tourDto.TourID))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Log the exception
                ModelState.AddModelError(string.Empty, $"An error occurred while updating the tour: {ex.Message}");
                
                // Reload the tour entity for the view (Edit view expects Tour, not TourUpdateDto)
                var tour = await _context.Tours
                    .Include(t => t.Itineraries)
                    .Include(t => t.TourPrices)
                    .Include(t => t.TourInstances)
                    .Include(t => t.TourServices)
                    .Include(t => t.TourImages)
                    .Include(t => t.Location)
                    .FirstOrDefaultAsync(m => m.TourID == id);
                
                // Reload data for the view
                var locations = await _context.Locations.ToListAsync();
                var categories = await _context.Categories.ToListAsync();
                var guides = await _context.Users.ToListAsync();
                var countries = await _context.Countries.ToListAsync();
                var services = await _context.Services.ToListAsync();

                ViewBag.Locations = locations;
                ViewBag.Categories = categories;
                ViewBag.Guides = guides;
                ViewBag.Countries = countries;
                ViewBag.Services = services;

                return View(tour ?? new Tour { TourID = id });
            }
        }

        // GET: Tour/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tour = await _context.Tours
                .FirstOrDefaultAsync(m => m.TourID == id);
            if (tour == null)
            {
                return NotFound();
            }

            return View(tour);
        }

        // POST: Tour/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var tour = await _context.Tours.FindAsync(id);
            if (tour != null)
            {
                _context.Tours.Remove(tour);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Tour/TourInstances/5
        public async Task<IActionResult> TourInstances(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tour = await _context.Tours
                .Include(t => t.TourInstances)
                .ThenInclude(ti => ti.Guide)
                .FirstOrDefaultAsync(m => m.TourID == id);

            if (tour == null)
            {
                return NotFound();
            }

            return View(tour);
        }

        // GET: Tour/CreateInstance/5
        public async Task<IActionResult> CreateInstance(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tour = await _context.Tours.FindAsync(id);
            if (tour == null)
            {
                return NotFound();
            }

            var guides = await _context.Users.ToListAsync();
            ViewBag.Guides = guides;

            return View(new TourInstance { TourID = id.Value });
        }

        // POST: Tour/CreateInstance
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateInstance(TourInstance instance)
        {
            if (ModelState.IsValid)
            {
                _context.TourInstances.Add(instance);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(TourInstances), new { id = instance.TourID });
            }

            var guides = await _context.Users.ToListAsync();
            ViewBag.Guides = guides;

            return View(instance);
        }

        // GET: Tour/EditInstance/5
        public async Task<IActionResult> EditInstance(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var instance = await _context.TourInstances
                .FirstOrDefaultAsync(m => m.InstanceID == id);
            if (instance == null)
            {
                return NotFound();
            }

            var guides = await _context.Users.ToListAsync();
            ViewBag.Guides = guides;

            return View(instance);
        }

        // POST: Tour/EditInstance/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditInstance(Guid id, TourInstance instance)
        {
            if (id != instance.InstanceID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(instance);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TourInstanceExists(instance.InstanceID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(TourInstances), new { id = instance.TourID });
            }

            var guides = await _context.Users.ToListAsync();
            ViewBag.Guides = guides;

            return View(instance);
        }

        // GET: Tour/DeleteInstance/5
        public async Task<IActionResult> DeleteInstance(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var instance = await _context.TourInstances
                .FirstOrDefaultAsync(m => m.InstanceID == id);
            if (instance == null)
            {
                return NotFound();
            }

            return View(instance);
        }

        // POST: Tour/DeleteInstance/5
        [HttpPost, ActionName("DeleteInstance")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteInstanceConfirmed(Guid id)
        {
            var instance = await _context.TourInstances.FindAsync(id);
            if (instance != null)
            {
                _context.TourInstances.Remove(instance);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(TourInstances), new { id = instance?.TourID });
        }

        private bool TourExists(Guid id)
        {
            return _context.Tours.Any(e => e.TourID == id);
        }

        private bool TourInstanceExists(Guid id)
        {
            return _context.TourInstances.Any(e => e.InstanceID == id);
        }

        public async Task<IActionResult> Index()
        {
            var tours = await _context.Tours
                .Include(t => t.Location!)
                    .ThenInclude(l => l!.Country!)
                .Include(t => t.Category)
                .Include(t => t.DefaultGuide)
                .ToListAsync();

            return View(tours);
        }
    }
}