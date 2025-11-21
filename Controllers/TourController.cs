using Microsoft.AspNetCore.Mvc;
using TourViet.Data;
using TourViet.Models;
using Microsoft.EntityFrameworkCore;

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
            string? NewLocationDescription, Guid? CountryID)
        {
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

            if (ModelState.IsValid)
            {
                _context.Add(tour);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Nếu có lỗi, load lại dữ liệu cho view
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
                    _context.Update(tour);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TourExists(tour.TourID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
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