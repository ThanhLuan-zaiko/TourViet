using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourViet.Data;
using TourViet.Models;
using TourViet.Services.Interfaces;

namespace TourViet.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly TourBookingDbContext _context;
    private readonly ITourService _tourService;

    public HomeController(
        ILogger<HomeController> logger, 
        TourBookingDbContext context,
        ITourService tourService)
    {
        _logger = logger;
        _context = context;
        _tourService = tourService;
    }

    public async Task<IActionResult> Index()
    {
        // Check if user is logged in and has AdministrativeStaff role
        var userRoles = HttpContext.Session.GetString("Roles")?.Split(',') ?? new string[0];
        var isAdministrativeStaff = userRoles.Contains("AdministrativeStaff");
        
        if (isAdministrativeStaff)
        {
            // Redirect AdministrativeStaff to their dashboard
            return RedirectToAction("StaffDashboard");
        }
        
        var publishedTours = await _tourService.GetPublishedToursAsync();
        return View(publishedTours);
    }

    // GET: Home/TourDetails/5
    public async Task<IActionResult> TourDetails(Guid? id)
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
            .Where(t => !t.IsDeleted && t.IsPublished) // Only show published tours to public
            .FirstOrDefaultAsync(m => m.TourID == id);
        
        if (tour == null)
        {
            return NotFound();
        }

        return View(tour);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
    
    // AdministrativeStaff specific actions
    public IActionResult StaffDashboard()
    {
        var userRoles = HttpContext.Session.GetString("Roles")?.Split(',') ?? new string[0];
        var isAdministrativeStaff = userRoles.Contains("AdministrativeStaff");
        
        if (!isAdministrativeStaff)
        {
            return RedirectToAction("Index");
        }
        
        return View("../AdministrativeStaffPage/StaffDashboard");
    }

    public async Task<IActionResult> ManageTours()
    {
        var userRoles = HttpContext.Session.GetString("Roles")?.Split(',') ?? new string[0];
        var isAdministrativeStaff = userRoles.Contains("AdministrativeStaff");
        
        if (!isAdministrativeStaff)
        {
            return RedirectToAction("Index");
        }
        
        // Get all tours with related data for DataTables to handle client-side
        var tours = await _context.Tours
            .Include(t => t.Location)
            .Include(t => t.Category)
            .Where(t => !t.IsDeleted)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
        
        return View("../AdministrativeStaffPage/ManageTours", tours);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTour(Guid id)
    {
        var userRoles = HttpContext.Session.GetString("Roles")?.Split(',') ?? new string[0];
        var isAdministrativeStaff = userRoles.Contains("AdministrativeStaff");
        
        if (!isAdministrativeStaff)
        {
            return Json(new { success = false, message = "Bạn không có quyền thực hiện hành động này." });
        }
        
        try
        {
            var result = await _tourService.DeleteTourAsync(id);
            
            if (!result)
            {
                return Json(new { success = false, message = "Tour không tồn tại hoặc đã bị xóa." });
            }
            
            return Json(new { success = true, message = "Tour đã được xóa thành công!" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting tour {TourId}", id);
            return Json(new { success = false, message = "Có lỗi xảy ra khi xóa tour." });
        }
    }

    public IActionResult ManageBookings()
    {
        var userRoles = HttpContext.Session.GetString("Roles")?.Split(',') ?? new string[0];
        var isAdministrativeStaff = userRoles.Contains("AdministrativeStaff");
        
        if (!isAdministrativeStaff)
        {
            return RedirectToAction("Index");
        }
        
        return View("../AdministrativeStaffPage/ManageBookings");
    }

    public IActionResult TourSchedule()
    {
        var userRoles = HttpContext.Session.GetString("Roles")?.Split(',') ?? new string[0];
        var isAdministrativeStaff = userRoles.Contains("AdministrativeStaff");
        
        if (!isAdministrativeStaff)
        {
            return RedirectToAction("Index");
        }
        
        return View("../AdministrativeStaffPage/TourSchedule");
    }

    public IActionResult CustomerList()
    {
        var userRoles = HttpContext.Session.GetString("Roles")?.Split(',') ?? new string[0];
        var isAdministrativeStaff = userRoles.Contains("AdministrativeStaff");
        
        if (!isAdministrativeStaff)
        {
            return RedirectToAction("Index");
        }
        
        return View("../AdministrativeStaffPage/CustomerList");
    }
}
