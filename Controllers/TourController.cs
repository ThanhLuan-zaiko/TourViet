using Microsoft.AspNetCore.Mvc;
using TourViet.Data;
using TourViet.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

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
                    .ThenInclude(l => l.Country)
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
        public async Task<IActionResult> Edit(
            Guid id, 
            Tour tour,
            string? NewCountryName,
            string? NewLocationName,
            string? NewLocationCity,
            string? NewLocationAddress,
            decimal? NewLocationLatitude,
            decimal? NewLocationLongitude,
            string? NewLocationDescription,
            List<Guid>? TourServiceIds,
            List<IFormFile>? TourImageFiles)
        {
            if (id != tour.TourID)
            {
                return NotFound();
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Get existing tour with all related entities
                var existingTour = await _context.Tours
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

                // Update basic tour properties
                existingTour.TourName = tour.TourName;
                existingTour.Slug = tour.Slug;
                existingTour.ShortDescription = tour.ShortDescription;
                existingTour.Description = tour.Description;
                existingTour.CategoryID = tour.CategoryID;
                existingTour.DefaultGuideID = tour.DefaultGuideID;
                existingTour.IsPublished = tour.IsPublished;
                existingTour.UpdatedAt = DateTime.UtcNow;

                // Handle Location
                if (!string.IsNullOrWhiteSpace(NewLocationName) && NewLocationLatitude.HasValue && NewLocationLongitude.HasValue)
                {
                    // Check if we should update existing location or create new one
                    if (existingTour.LocationID.HasValue && existingTour.Location != null)
                    {
                        // Update existing location
                        existingTour.Location.LocationName = NewLocationName;
                        existingTour.Location.City = NewLocationCity;
                        existingTour.Location.Address = NewLocationAddress;
                        existingTour.Location.Latitude = NewLocationLatitude;
                        existingTour.Location.Longitude = NewLocationLongitude;
                        existingTour.Location.Description = NewLocationDescription;
                        existingTour.Location.UpdatedAt = DateTime.UtcNow;

                        // Update country if provided
                        if (!string.IsNullOrWhiteSpace(NewCountryName))
                        {
                            var country = await _context.Countries.FirstOrDefaultAsync(c => c.CountryName == NewCountryName);
                            if (country == null)
                            {
                                // Generate ISO3 from country name (first 3 letters uppercase)
                                var iso3 = NewCountryName.Length >= 3 ? NewCountryName.Substring(0, 3).ToUpper() : NewCountryName.ToUpper();
                                country = new Country { 
                                    CountryName = NewCountryName,
                                    ISO3 = iso3 + "_" + Guid.NewGuid().ToString().Substring(0, 4) // Make it unique
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
                            LocationName = NewLocationName,
                            City = NewLocationCity,
                            Address = NewLocationAddress,
                            Latitude = NewLocationLatitude,
                            Longitude = NewLocationLongitude,
                            Description = NewLocationDescription
                        };

                        if (!string.IsNullOrWhiteSpace(NewCountryName))
                        {
                            var country = await _context.Countries.FirstOrDefaultAsync(c => c.CountryName == NewCountryName);
                            if (country == null)
                            {
                                // Generate ISO3 from country name (first 3 letters uppercase)
                                var iso3_2 = NewCountryName.Length >= 3 ? NewCountryName.Substring(0, 3).ToUpper() : NewCountryName.ToUpper();
                                country = new Country { 
                                    CountryName = NewCountryName,
                                    ISO3 = iso3_2 + "_" + Guid.NewGuid().ToString().Substring(0, 4) // Make it unique
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
                _context.TourServices.RemoveRange(existingTour.TourServices);
                if (TourServiceIds != null && TourServiceIds.Any())
                {
                    foreach (var serviceId in TourServiceIds)
                    {
                        existingTour.TourServices.Add(new TourService
                        {
                            TourID = existingTour.TourID,
                            ServiceID = serviceId
                        });
                    }
                }

                // Handle Images
                if (TourImageFiles != null && TourImageFiles.Any())
                {
                    var uploadDir = Path.Combine("Uploads", existingTour.TourID.ToString());
                    if (!Directory.Exists(uploadDir))
                    {
                        Directory.CreateDirectory(uploadDir);
                    }

                    foreach (var file in TourImageFiles)
                    {
                        if (file.Length > 0)
                        {
                            var fileName = $"{Guid.NewGuid()}.webp";
                            var filePath = Path.Combine(uploadDir, fileName);

                            using (var image = await Image.LoadAsync(file.OpenReadStream()))
                            {
                                await image.SaveAsWebpAsync(filePath);
                            }

                            existingTour.TourImages.Add(new TourImage
                            {
                                TourID = existingTour.TourID,
                                Url = $"/Uploads/{existingTour.TourID}/{fileName}",
                                MimeType = "image/webp",
                                SortOrder = existingTour.TourImages.Count + 1
                            });
                        }
                    }
                }

                // Update Itineraries - remove old and add new
                _context.Itineraries.RemoveRange(existingTour.Itineraries);
                if (tour.Itineraries != null && tour.Itineraries.Any())
                {
                    foreach (var itinerary in tour.Itineraries)
                    {
                        itinerary.TourID = existingTour.TourID;
                        if (itinerary.ItineraryID == Guid.Empty)
                        {
                            itinerary.ItineraryID = Guid.NewGuid();
                        }
                        existingTour.Itineraries.Add(itinerary);
                    }
                }

                // Update TourPrices - remove old and add new
                _context.TourPrices.RemoveRange(existingTour.TourPrices);
                if (tour.TourPrices != null && tour.TourPrices.Any())
                {
                    foreach (var price in tour.TourPrices)
                    {
                        price.TourID = existingTour.TourID;
                        if (price.TourPriceID == Guid.Empty)
                        {
                            price.TourPriceID = Guid.NewGuid();
                        }
                        if (string.IsNullOrEmpty(price.Currency))
                        {
                            price.Currency = "USD";
                        }
                        existingTour.TourPrices.Add(price);
                    }
                }

                // Update TourInstances - remove old and add new
                _context.TourInstances.RemoveRange(existingTour.TourInstances);
                if (tour.TourInstances != null && tour.TourInstances.Any())
                {
                    foreach (var instance in tour.TourInstances)
                    {
                        instance.TourID = existingTour.TourID;
                        if (instance.InstanceID == Guid.Empty)
                        {
                            instance.InstanceID = Guid.NewGuid();
                        }
                        if (string.IsNullOrEmpty(instance.Currency))
                        {
                            instance.Currency = "VND";
                        }
                        if (string.IsNullOrEmpty(instance.Status))
                        {
                            instance.Status = "Scheduled";
                        }
                        existingTour.TourInstances.Add(instance);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return RedirectToAction("ManageTours", "Home");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Error updating tour: {ex.Message}");
                Console.WriteLine($"Inner exception: {ex.InnerException?.Message}");

                // Reload ViewBag data for error case
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

                ModelState.AddModelError("", "Đã xảy ra lỗi khi cập nhật tour. Vui lòng kiểm tra lại thông tin.");
                return View(tour);
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