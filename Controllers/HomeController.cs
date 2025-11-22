using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourViet.Data;
using TourViet.Models;

namespace TourViet.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly TourBookingDbContext _context;

    public HomeController(ILogger<HomeController> logger, TourBookingDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public IActionResult Index()
    {
        // Check if user is logged in and has AdministrativeStaff role
        var userRoles = HttpContext.Session.GetString("Roles")?.Split(',') ?? new string[0];
        var isAdministrativeStaff = userRoles.Contains("AdministrativeStaff");
        
        if (isAdministrativeStaff)
        {
            // Redirect AdministrativeStaff to their dashboard
            return RedirectToAction("StaffDashboard");
        }
        
        return View();
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

    public async Task<IActionResult> ManageTours(int page = 1, string? search = null)
    {
        var userRoles = HttpContext.Session.GetString("Roles")?.Split(',') ?? new string[0];
        var isAdministrativeStaff = userRoles.Contains("AdministrativeStaff");
        
        if (!isAdministrativeStaff)
        {
            return RedirectToAction("Index");
        }
        
        const int pageSize = 10;
        
        // Build query
        var query = _context.Tours
            .Include(t => t.Location)
            .Include(t => t.Category)
            .Where(t => !t.IsDeleted)
            .AsQueryable();
        
        // Apply search filter
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(t => 
                t.TourName.Contains(search) || 
                (t.Location != null && t.Location.LocationName.Contains(search)) ||
                (t.ShortDescription != null && t.ShortDescription.Contains(search))
            );
        }
        
        // Get total count for pagination
        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        
        // Ensure page is within valid range
        page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));
        
        // Get paginated tours
        var tours = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        // Pass data to view
        ViewBag.Tours = tours;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.Search = search;
        ViewBag.TotalItems = totalItems;
        
        return View("../AdministrativeStaffPage/ManageTours");
    }

    [HttpPost]
    public async Task<IActionResult> SearchTours(string search, int page = 1)
    {
        var userRoles = HttpContext.Session.GetString("Roles")?.Split(',') ?? new string[0];
        var isAdministrativeStaff = userRoles.Contains("AdministrativeStaff");
        
        if (!isAdministrativeStaff)
        {
            return Json(new { success = false, message = "Unauthorized" });
        }
        
        const int pageSize = 10;
        
        var query = _context.Tours
            .Include(t => t.Location)
            .Include(t => t.Category)
            .Where(t => !t.IsDeleted)
            .AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(t => 
                t.TourName.Contains(search) || 
                (t.Location != null && t.Location.LocationName.Contains(search)) ||
                (t.ShortDescription != null && t.ShortDescription.Contains(search))
            );
        }
        
        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));
        
        var tours = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        return Json(new { 
            success = true, 
            tours = tours.Select(t => new {
                tourID = t.TourID,
                tourName = t.TourName,
                locationName = t.Location?.LocationName ?? "Chưa có địa điểm",
                shortDescription = string.IsNullOrEmpty(t.ShortDescription) ? "Chưa có mô tả" : t.ShortDescription,
                isPublished = t.IsPublished,
                createdAt = t.CreatedAt.ToString("dd/MM/yyyy")
            }),
            currentPage = page,
            totalPages = totalPages,
            totalItems = totalItems
        });
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
            var tour = await _context.Tours.FindAsync(id);
            
            if (tour == null || tour.IsDeleted)
            {
                return Json(new { success = false, message = "Tour không tồn tại hoặc đã bị xóa." });
            }
            
            // Soft delete
            tour.IsDeleted = true;
            tour.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            
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
