using Microsoft.AspNetCore.Mvc;
using TourViet.Data;
using TourViet.Models;
using Microsoft.EntityFrameworkCore;
using TourViet.Models.DTOs;
using TourViet.Services.Interfaces;

namespace TourViet.Controllers
{
    public class TourController : Controller
    {
        private readonly TourBookingDbContext _context;
        private readonly ITourService _tourService;
        private readonly IImageService _imageService;
        private readonly ILogger<TourController> _logger;

        public TourController(
            TourBookingDbContext context,
            ITourService tourService,
            IImageService imageService,
            ILogger<TourController> logger)
        {
            _context = context;
            _tourService = tourService;
            _imageService = imageService;
            _logger = logger;
        }

        // GET: Tour/Create
        public async Task<IActionResult> Create()
        {
            await LoadViewDataAsync();
            return View(new TourCreateDto());
        }

        // POST: Tour/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] TourCreateDto tourDto)
        {
            // Remove validation errors for navigation properties (they will be set by EF Core)
            var keysToRemove = ModelState.Keys
                .Where(k => k.Contains(".Tour") || k.Contains(".TourID"))
                .ToList();
            
            foreach (var key in keysToRemove)
            {
                ModelState.Remove(key);
            }
            
            try
            {
                if (!ModelState.IsValid)
                {
                    await LoadViewDataAsync();
                    return View(tourDto);
                }

                // Create tour using service
                var tour = await _tourService.CreateTourAsync(tourDto, Request.Form.Files);

                return RedirectToAction("ManageTours", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating tour");
                ModelState.AddModelError(string.Empty, $"Lỗi tạo tour: {ex.Message}");
                
                await LoadViewDataAsync();
                return View(tourDto);
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

            // Remove validation errors for navigation properties (they will be set by EF Core)
            var keysToRemove = ModelState.Keys
                .Where(k => k.Contains(".Tour") || k.Contains(".TourID"))
                .ToList();
            
            foreach (var key in keysToRemove)
            {
                ModelState.Remove(key);
            }

            try
            {
                if (!ModelState.IsValid)
                {
                    await LoadViewDataAsync();
                    var tour = await _context.Tours
                        .Include(t => t.Itineraries)
                        .Include(t => t.TourPrices)
                        .Include(t => t.TourInstances)
                        .Include(t => t.TourServices)
                        .Include(t => t.TourImages)
                        .Include(t => t.Location)
                        .FirstOrDefaultAsync(m => m.TourID == id);
                    return View(tour);
                }

                // Update tour using service
                await _tourService.UpdateTourAsync(id, tourDto, Request.Form.Files);

                return RedirectToAction("ManageTours", "Home");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _tourService.TourExistsAsync(tourDto.TourID))
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
                ModelState.AddModelError(string.Empty, $"Lỗi cập nhật tour: {ex.Message}");
                
                await LoadViewDataAsync();
                var tour = await _context.Tours
                    .Include(t => t.Itineraries)
                    .Include(t => t.TourPrices)
                    .Include(t => t.TourInstances)
                    .Include(t => t.TourServices)
                    .Include(t => t.TourImages)
                    .Include(t => t.Location)
                    .FirstOrDefaultAsync(m => m.TourID == id);

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

        private bool TourInstanceExists(Guid id)
        {
            return _context.TourInstances.Any(e => e.InstanceID == id);
        }

        [HttpPost]
        public async Task<IActionResult> UploadImages(Guid id, IFormFileCollection files)
        {
            try
            {
                if (files == null || files.Count == 0)
                    return Json(new { success = false, message = "Không có file nào được chọn" });

                var maxSortOrder = await _imageService.GetMaxSortOrderAsync(id);
                var images = await _imageService.ProcessMultipleImagesAsync(id, files, maxSortOrder + 1);
                
                foreach (var img in images)
                {
                    _context.TourImages.Add(img);
                }
                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    images = images.Select(i => new { 
                        id = i.ImageID, 
                        url = i.Url, 
                        fileName = i.FileName,
                        isPrimary = i.IsPrimary,
                        sortOrder = i.SortOrder
                    }) 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteImage(Guid id)
        {
            try
            {
                var result = await _imageService.DeleteImageAsync(id);
                return Json(new { success = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Helper method to load common ViewBag data for Create/Edit forms
        /// </summary>
        private async Task LoadViewDataAsync()
        {
            ViewBag.Locations = await _context.Locations
                .Include(l => l.Country)
                .ToListAsync();
            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.Guides = await _context.Users.ToListAsync();
            ViewBag.Countries = await _context.Countries.ToListAsync();
            ViewBag.Services = await _context.Services.ToListAsync();
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