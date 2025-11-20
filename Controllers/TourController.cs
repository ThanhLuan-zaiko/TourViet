using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourViet.Data;
using TourViet.Models;
using TourViet.Services;
using TourService = TourViet.Models.TourService;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace TourViet.Controllers;

public class TourController : Controller
{
    private readonly ITourService _tourService;
    private readonly ITourInstanceService _tourInstanceService;
    private readonly TourBookingDbContext _context;

    public TourController(ITourService tourService, ITourInstanceService tourInstanceService, TourBookingDbContext context)
    {
        _tourService = tourService;
        _tourInstanceService = tourInstanceService;
        _context = context;
    }

    // GET: Tour
    public async Task<IActionResult> Index()
    {
        var tours = await _tourService.GetAllToursAsync();
        ViewBag.Tours = tours;
        return View();
    }

    // GET: Tour/Details/5
    public async Task<IActionResult> Details(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var tour = await _tourService.GetTourByIdAsync(id.Value);
        if (tour == null)
        {
            return NotFound();
        }

        return View(tour);
    }

    // GET: Tour/Create
    public async Task<IActionResult> Create()
    {
        // Load data for dropdowns
        ViewBag.Locations = await _context.Locations.Where(l => !l.IsDeleted).ToListAsync();
        ViewBag.Categories = await _context.Categories.ToListAsync();
        ViewBag.Guides = await _context.Users.Where(u => u.UserRoles.Any(ur => ur.Role.RoleName == "AdministrativeStaff" || ur.Role.RoleName == "ExecutiveStaff")).ToListAsync();
        ViewBag.Countries = await _context.Countries.ToListAsync();
        ViewBag.Services = await _context.Services.Where(s => !s.IsDeleted).ToListAsync();

        return View();
    }

    // POST: Tour/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        Tour tour,
        Guid? CountryID,
        string NewLocationName,
        string NewLocationCity,
        string NewLocationAddress,
        decimal? NewLocationLatitude,
        decimal? NewLocationLongitude,
        string NewLocationDescription,
        List<TourInstance> TourInstances,
        List<TourPrice> TourPrices,
        List<Itinerary> Itineraries,
        List<Guid> TourServiceIds,
        IFormFileCollection files)
    {
        if (ModelState.IsValid)
        {
            // Generate slug from tour name if not provided
            if (string.IsNullOrEmpty(tour.Slug))
            {
                tour.Slug = GenerateSlug(tour.TourName);
            }
            
            // Handle new location creation if provided
            if (!string.IsNullOrEmpty(NewLocationName))
            {
                var newLocation = new Location
                {
                    LocationName = NewLocationName,
                    CountryID = CountryID,
                    City = NewLocationCity,
                    Address = NewLocationAddress,
                    Latitude = NewLocationLatitude,
                    Longitude = NewLocationLongitude,
                    Description = NewLocationDescription,
                    CreatedAt = DateTime.UtcNow
                };
                
                _context.Locations.Add(newLocation);
                await _context.SaveChangesAsync();
                
                // Set the tour's LocationID to the new location
                tour.LocationID = newLocation.LocationID;
            }
            
            // Create the tour first
            await _tourService.CreateTourAsync(tour);
            
            // Add tour instances if provided
            if (TourInstances != null)
            {
                foreach (var instance in TourInstances)
                {
                    if (instance.StartDate != default && instance.EndDate != default && instance.StartDate < instance.EndDate)
                    {
                        instance.TourID = tour.TourID;
                        instance.CreatedAt = DateTime.UtcNow;
                        _context.TourInstances.Add(instance);
                    }
                }
            }
            
            // Add tour prices if provided
            if (TourPrices != null)
            {
                foreach (var price in TourPrices)
                {
                    if (!string.IsNullOrEmpty(price.PriceType) && price.Amount > 0)
                    {
                        price.TourID = tour.TourID;
                        _context.TourPrices.Add(price);
                    }
                }
            }
            
            // Add itineraries if provided
            if (Itineraries != null)
            {
                foreach (var itinerary in Itineraries)
                {
                    if (!string.IsNullOrEmpty(itinerary.Title))
                    {
                        itinerary.TourID = tour.TourID;
                        _context.Itineraries.Add(itinerary);
                    }
                }
            }
            
            // Add tour services if provided
            if (TourServiceIds != null)
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
                    _context.TourServices.Add(tourService);
                }
            }
            
            // Handle image uploads if provided
            if (files != null && files.Count > 0)
            {
                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", tour.TourID.ToString());
                Directory.CreateDirectory(uploadsPath);
                
                foreach (var file in files)
                {
                    if (file.Length > 0 && file.ContentType.StartsWith("image/"))
                    {
                        var originalFileName = Path.GetFileNameWithoutExtension(file.FileName);
                        var fileExtension = Path.GetExtension(file.FileName);
                        var webpFileName = $"{originalFileName}.webp";
                        var filePath = Path.Combine(uploadsPath, webpFileName);
                        
                        int imageWidth = 0;
                        int imageHeight = 0;
                        
                        // Convert image to WebP and save
                        using (var inputStream = file.OpenReadStream())
                        using (var output = new FileStream(filePath, FileMode.Create))
                        using (var image = Image.Load(inputStream))
                        {
                            // Get image dimensions
                            imageWidth = image.Width;
                            imageHeight = image.Height;
                            
                            // Convert and save as WebP
                            image.Save(output, new WebpEncoder()
                            {
                                Quality = 80, // Set quality for WebP compression
                                FileFormat = WebpFileFormatType.Lossy // Use lossy compression for smaller file sizes
                            });
                        }
                        
                        // Create TourImage record
                        var tourImage = new TourImage
                        {
                            TourID = tour.TourID,
                            FileName = webpFileName,
                            Path = Path.Combine("uploads", tour.TourID.ToString(), webpFileName),
                            Url = $"/uploads/{tour.TourID}/{webpFileName}",
                            MimeType = "image/webp",
                            FileSize = (int)new FileInfo(filePath).Length, // Get actual file size after conversion
                            Width = imageWidth, // Get actual width after conversion
                            Height = imageHeight, // Get actual height after conversion
                            UploadedAt = DateTime.UtcNow,
                            IsPublic = true
                        };
                        
                        _context.TourImages.Add(tourImage);
                    }
                }
            }
            
            // Save all related entities
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Tour created successfully!";
            return RedirectToAction(nameof(Index));
        }

        // Load data for dropdowns if model is invalid
        ViewBag.Locations = await _context.Locations.Where(l => !l.IsDeleted).ToListAsync();
        ViewBag.Categories = await _context.Categories.ToListAsync();
        ViewBag.Guides = await _context.Users.Where(u => u.UserRoles.Any(ur => ur.Role.RoleName == "AdministrativeStaff" || ur.Role.RoleName == "ExecutiveStaff")).ToListAsync();
        ViewBag.Countries = await _context.Countries.ToListAsync();
        ViewBag.Services = await _context.Services.Where(s => !s.IsDeleted).ToListAsync();

        return View(tour);
    }

    // GET: Tour/Edit/5
    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var tour = await _tourService.GetTourByIdAsync(id.Value);
        if (tour == null)
        {
            return NotFound();
        }

        // Load data for dropdowns
        ViewBag.Locations = await _context.Locations.Where(l => !l.IsDeleted).ToListAsync();
        ViewBag.Categories = await _context.Categories.ToListAsync();
        ViewBag.Guides = await _context.Users.Where(u => u.UserRoles.Any(ur => ur.Role.RoleName == "AdministrativeStaff" || ur.Role.RoleName == "ExecutiveStaff")).ToListAsync();

        return View(tour);
    }

    // POST: Tour/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Tour tour)
    {
        if (id != tour.TourID)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                // Generate slug from tour name if not provided
                if (string.IsNullOrEmpty(tour.Slug))
                {
                    tour.Slug = GenerateSlug(tour.TourName);
                }
                
                await _tourService.UpdateTourAsync(tour);
                TempData["SuccessMessage"] = "Tour updated successfully!";
            }
            catch (ArgumentException)
            {
                return NotFound();
            }
            return RedirectToAction(nameof(Index));
        }

        // Load data for dropdowns if model is invalid
        ViewBag.Locations = await _context.Locations.Where(l => !l.IsDeleted).ToListAsync();
        ViewBag.Categories = await _context.Categories.ToListAsync();
        ViewBag.Guides = await _context.Users.Where(u => u.UserRoles.Any(ur => ur.Role.RoleName == "AdministrativeStaff" || ur.Role.RoleName == "ExecutiveStaff")).ToListAsync();

        return View(tour);
    }

    // GET: Tour/Delete/5
    public async Task<IActionResult> Delete(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var tour = await _tourService.GetTourByIdAsync(id.Value);
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
        var result = await _tourService.DeleteTourAsync(id);
        if (!result)
        {
            return NotFound();
        }
        
        TempData["SuccessMessage"] = "Tour deleted successfully!";
        return RedirectToAction(nameof(Index));
    }

    // GET: Tour/Instances/5
    public async Task<IActionResult> TourInstances(Guid? tourId)
    {
        if (tourId == null)
        {
            return NotFound();
        }

        var tour = await _tourService.GetTourByIdAsync(tourId.Value);
        if (tour == null)
        {
            return NotFound();
        }

        var instances = await _tourInstanceService.GetTourInstancesByTourIdAsync(tourId.Value);
        ViewBag.Tour = tour;

        return View(instances);
    }

    // GET: Tour/CreateInstance/5
    public async Task<IActionResult> CreateInstance(Guid? tourId)
    {
        if (tourId == null)
        {
            return NotFound();
        }

        var tour = await _tourService.GetTourByIdAsync(tourId.Value);
        if (tour == null)
        {
            return NotFound();
        }

        ViewBag.Tour = tour;
        ViewBag.Guides = await _context.Users.Where(u => u.UserRoles.Any(ur => ur.Role.RoleName == "AdministrativeStaff" || ur.Role.RoleName == "ExecutiveStaff")).ToListAsync();

        var tourInstance = new TourInstance
        {
            TourID = tourId.Value,
            Tour = tour
        };

        return View(tourInstance);
    }

    // POST: Tour/CreateInstance
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateInstance(TourInstance tourInstance)
    {
        if (ModelState.IsValid)
        {
            // Validate that EndDate is after StartDate
            if (tourInstance.EndDate <= tourInstance.StartDate)
            {
                ModelState.AddModelError("EndDate", "Ngày kết thúc phải sau ngày bắt đầu.");
            }
            else
            {
                await _tourInstanceService.CreateTourInstanceAsync(tourInstance);
                TempData["SuccessMessage"] = "Tour instance created successfully!";
                return RedirectToAction(nameof(TourInstances), new { tourId = tourInstance.TourID });
            }
        }

        // Load data for dropdowns if model is invalid
        ViewBag.Tour = await _tourService.GetTourByIdAsync(tourInstance.TourID);
        ViewBag.Guides = await _context.Users.Where(u => u.UserRoles.Any(ur => ur.Role.RoleName == "AdministrativeStaff" || ur.Role.RoleName == "ExecutiveStaff")).ToListAsync();

        return View(tourInstance);
    }

    // GET: Tour/EditInstance/5
    public async Task<IActionResult> EditInstance(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var tourInstance = await _tourInstanceService.GetTourInstanceByIdAsync(id.Value);
        if (tourInstance == null)
        {
            return NotFound();
        }

        ViewBag.Guides = await _context.Users.Where(u => u.UserRoles.Any(ur => ur.Role.RoleName == "AdministrativeStaff" || ur.Role.RoleName == "ExecutiveStaff")).ToListAsync();

        return View(tourInstance);
    }

    // POST: Tour/EditInstance/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditInstance(Guid id, TourInstance tourInstance)
    {
        if (id != tourInstance.InstanceID)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                // Validate that EndDate is after StartDate
                if (tourInstance.EndDate <= tourInstance.StartDate)
                {
                    ModelState.AddModelError("EndDate", "Ngày kết thúc phải sau ngày bắt đầu.");
                }
                else
                {
                    await _tourInstanceService.UpdateTourInstanceAsync(tourInstance);
                    TempData["SuccessMessage"] = "Tour instance updated successfully!";
                    return RedirectToAction(nameof(TourInstances), new { tourId = tourInstance.TourID });
                }
            }
            catch (ArgumentException)
            {
                return NotFound();
            }
        }

        // Load data for dropdowns if model is invalid
        ViewBag.Guides = await _context.Users.Where(u => u.UserRoles.Any(ur => ur.Role.RoleName == "AdministrativeStaff" || ur.Role.RoleName == "ExecutiveStaff")).ToListAsync();
        var tourData = await _tourService.GetTourByIdAsync(tourInstance.TourID);
        if (tourData != null)
        {
            tourInstance.Tour = tourData;
        }

        return View(tourInstance);
    }

    // GET: Tour/DeleteInstance/5
    public async Task<IActionResult> DeleteInstance(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var tourInstance = await _tourInstanceService.GetTourInstanceByIdAsync(id.Value);
        if (tourInstance == null)
        {
            return NotFound();
        }

        return View(tourInstance);
    }

    // POST: Tour/DeleteInstance/5
    [HttpPost, ActionName("DeleteInstance")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteInstanceConfirmed(Guid id)
    {
        var tourInstance = await _tourInstanceService.GetTourInstanceByIdAsync(id);
        if (tourInstance == null)
        {
            return NotFound();
        }

        var tourId = tourInstance.TourID;
        var result = await _tourInstanceService.DeleteTourInstanceAsync(id);
        if (!result)
        {
            return NotFound();
        }

        TempData["SuccessMessage"] = "Tour instance deleted successfully!";
        return RedirectToAction(nameof(TourInstances), new { tourId = tourId });
    }

    private string GenerateSlug(string phrase)
    {
        string str = phrase.ToLower();
        // Remove invalid characters
        str = System.Text.RegularExpressions.Regex.Replace(str, @"[^a-z0-9\s-]", "");
        // Remove multiple spaces/hyphens
        str = System.Text.RegularExpressions.Regex.Replace(str, @"[\s-]+", " ").Trim();
        // Replace spaces with hyphens
        str = System.Text.RegularExpressions.Regex.Replace(str, @"\s", "-");
        return str;
    }
}