using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourViet.Data;
using TourViet.Models;
using TourViet.Services;

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

        return View();
    }

    // POST: Tour/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Tour tour)
    {
        if (ModelState.IsValid)
        {
            // Generate slug from tour name if not provided
            if (string.IsNullOrEmpty(tour.Slug))
            {
                tour.Slug = GenerateSlug(tour.TourName);
            }
            
            await _tourService.CreateTourAsync(tour);
            TempData["SuccessMessage"] = "Tour created successfully!";
            return RedirectToAction(nameof(Index));
        }

        // Load data for dropdowns if model is invalid
        ViewBag.Locations = await _context.Locations.Where(l => !l.IsDeleted).ToListAsync();
        ViewBag.Categories = await _context.Categories.ToListAsync();
        ViewBag.Guides = await _context.Users.Where(u => u.UserRoles.Any(ur => ur.Role.RoleName == "AdministrativeStaff" || ur.Role.RoleName == "ExecutiveStaff")).ToListAsync();

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
        tourInstance.Tour = await _tourService.GetTourByIdAsync(tourInstance.TourID);

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