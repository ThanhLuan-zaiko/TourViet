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
    private readonly IBookingService _bookingService;

    public HomeController(
        ILogger<HomeController> logger, 
        TourBookingDbContext context,
        ITourService tourService,
        IBookingService bookingService)
    {
        _logger = logger;
        _context = context;
        _tourService = tourService;
        _bookingService = bookingService;
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
        
        // Initial load: Page 1, Size 6
        var publishedTours = await _tourService.GetPublishedToursPagedAsync(1, 6);
        return View(publishedTours);
    }

    [HttpGet]
    public async Task<IActionResult> GetToursPartial(int page = 1, int pageSize = 6)
    {
        var tours = await _tourService.GetPublishedToursPagedAsync(page, pageSize);
        
        if (tours == null || !tours.Any())
        {
            return NoContent();
        }
        
        return PartialView("_TourListPartial", tours);
    }

    public async Task<IActionResult> Trending()
    {
        // Initial load: Page 1, Size 6
        var trendingTours = await _tourService.GetTrendingToursPagedAsync(1, 6);
        return View(trendingTours);
    }

    [HttpGet]
    public async Task<IActionResult> GetTrendingToursPartial(int page = 1, int pageSize = 6)
    {
        var tours = await _tourService.GetTrendingToursPagedAsync(page, pageSize);
        
        if (tours == null || !tours.Any())
        {
            return NoContent();
        }
        
        return PartialView("_TourListPartial", tours);
    }

    public async Task<IActionResult> Domestic()
    {
        // Initial load: Page 1, Size 6
        var domesticTours = await _tourService.GetDomesticToursPagedAsync(1, 6);
        return View(domesticTours);
    }

    [HttpGet]
    public async Task<IActionResult> GetDomesticToursPartial(int page = 1, int pageSize = 6)
    {
        var tours = await _tourService.GetDomesticToursPagedAsync(page, pageSize);
        
        if (tours == null || !tours.Any())
        {
            return NoContent();
        }
        
        return PartialView("_TourListPartial", tours);
    }

    public async Task<IActionResult> International()
    {
        // Get list of available countries for filter
        var countries = await _tourService.GetInternationalCountriesAsync();
        ViewBag.Countries = countries;
        
        // Initial load: Page 1, Size 6, All countries
        var internationalTours = await _tourService.GetInternationalToursPagedAsync(1, 6);
        return View(internationalTours);
    }

    [HttpGet]
    public async Task<IActionResult> GetInternationalToursPartial(int page = 1, int pageSize = 6, Guid? countryId = null)
    {
        var tours = await _tourService.GetInternationalToursPagedAsync(page, pageSize, countryId);
        
        if (tours == null || !tours.Any())
        {
            return NoContent();
        }
        
        return PartialView("_TourListPartial", tours);
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
    public async Task<IActionResult> StaffDashboard()
    {
        var userRoles = HttpContext.Session.GetString("Roles")?.Split(',') ?? new string[0];
        var isAdministrativeStaff = userRoles.Contains("AdministrativeStaff");
        
        if (!isAdministrativeStaff)
        {
            return RedirectToAction("Index");
        }
        
        var statistics = new TourViet.ViewModels.DashboardStatisticsViewModel();
        var now = DateTime.UtcNow;
        var currentMonthStart = new DateTime(now.Year, now.Month, 1);
        var twelveMonthsAgo = currentMonthStart.AddMonths(-12);
        var thirtyDaysAgo = now.AddDays(-30);
        
        statistics.TotalTours = await _context.Tours.Where(t => !t.IsDeleted).CountAsync();
        statistics.TotalBookings = await _context.Bookings.CountAsync();
        statistics.TotalCustomers = await _context.Users.Where(u => u.UserRoles.Any(ur => ur.Role.RoleName == "Customer")).CountAsync();
        statistics.UpcomingTours = await _context.TourInstances.Where(ti => ti.StartDate > now && (ti.Capacity - ti.SeatsBooked - ti.SeatsHeld) > 0).CountAsync();
        
        var allBookings = await _context.Bookings.Select(b => new { b.TotalAmount, b.CreatedAt, b.Status }).ToListAsync();
        statistics.TotalRevenue = allBookings.Where(b => b.Status != "Cancelled").Sum(b => b.TotalAmount);
        statistics.CurrentMonthRevenue = allBookings.Where(b => b.CreatedAt >= currentMonthStart && b.Status != "Cancelled").Sum(b => b.TotalAmount);
        
        statistics.BookingsByStatus = await _context.Bookings.GroupBy(b => b.Status).Select(g => new { Status = g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Status, x => x.Count);
        statistics.TopTours = await _context.Bookings.Include(b => b.TourInstance).ThenInclude(ti => ti.Tour).Where(b => b.Status != "Cancelled").GroupBy(b => new { b.TourInstance.Tour.TourID, b.TourInstance.Tour.TourName }).Select(g => new TourViet.ViewModels.TopTourViewModel { TourName = g.Key.TourName, BookingCount = g.Count(), TotalRevenue = g.Sum(b => b.TotalAmount) }).OrderByDescending(t => t.BookingCount).Take(5).ToListAsync();
        
        var monthlyRevenueData = await _context.Bookings.Where(b => b.CreatedAt >= twelveMonthsAgo && b.Status != "Cancelled").GroupBy(b => new { b.CreatedAt.Year, b.CreatedAt.Month }).Select(g => new TourViet.ViewModels.MonthlyDataViewModel { Year = g.Key.Year, Month = g.Key.Month.ToString(), Value = g.Sum(b => b.TotalAmount), Count = g.Count() }).ToListAsync();
        statistics.MonthlyRevenue = new List<TourViet.ViewModels.MonthlyDataViewModel>();
        for (int i = 11; i >= 0; i--) { var date = currentMonthStart.AddMonths(-i); var data = monthlyRevenueData.FirstOrDefault(d => d.Year == date.Year && d.Month == date.Month.ToString()); statistics.MonthlyRevenue.Add(new TourViet.ViewModels.MonthlyDataViewModel { Year = date.Year, Month = date.ToString("MMM yyyy"), Value = data?.Value ?? 0, Count = data?.Count ?? 0 }); }
        statistics.MonthlyBookings = statistics.MonthlyRevenue.Select(m => new TourViet.ViewModels.MonthlyDataViewModel { Month = m.Month, Year = m.Year, Count = m.Count, Value = m.Value }).ToList();
        
        var customerGrowthData = await _context.Users.Where(u => u.CreatedAt >= twelveMonthsAgo && u.UserRoles.Any(ur => ur.Role.RoleName == "Customer")).GroupBy(u => new { u.CreatedAt.Year, u.CreatedAt.Month }).Select(g => new TourViet.ViewModels.MonthlyDataViewModel { Year = g.Key.Year, Month = g.Key.Month.ToString(), Count = g.Count() }).ToListAsync();
        statistics.CustomerGrowth = new List<TourViet.ViewModels.MonthlyDataViewModel>();
        for (int i = 11; i >= 0; i--) { var date = currentMonthStart.AddMonths(-i); var data = customerGrowthData.FirstOrDefault(d => d.Year == date.Year && d.Month == date.Month.ToString()); statistics.CustomerGrowth.Add(new TourViet.ViewModels.MonthlyDataViewModel { Year = date.Year, Month = date.ToString("MMM yyyy"), Count = data?.Count ?? 0 }); }
        
        statistics.ToursByCategory = await _context.Tours.Include(t => t.Category).Where(t => !t.IsDeleted && t.Category != null).GroupBy(t => t.Category!.CategoryName).Select(g => new { Category = g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Category, x => x.Count);
        
        var dailyBookingsData = await _context.Bookings.Where(b => b.CreatedAt >= thirtyDaysAgo).GroupBy(b => b.CreatedAt.Date).Select(g => new TourViet.ViewModels.DailyDataViewModel { Date = g.Key, Count = g.Count() }).ToListAsync();
        statistics.DailyBookings = new List<TourViet.ViewModels.DailyDataViewModel>();
        for (int i = 29; i >= 0; i--) { var date = now.Date.AddDays(-i); var data = dailyBookingsData.FirstOrDefault(d => d.Date == date); statistics.DailyBookings.Add(new TourViet.ViewModels.DailyDataViewModel { Date = date, Count = data?.Count ?? 0 }); }
        
        return View("../AdministrativeStaffPage/StaffDashboard", statistics);
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

    public async Task<IActionResult> ManageBookings()
    {
        var userRoles = HttpContext.Session.GetString("Roles")?.Split(',') ?? new string[0];
        var isAdministrativeStaff = userRoles.Contains("AdministrativeStaff");
        
        if (!isAdministrativeStaff)
        {
            return RedirectToAction("Index");
        }
        
        // Fetch all bookings with complete details
        var bookingsDto = await _bookingService.GetAllBookingsAsync();
        
        // Transform to ViewModel
        var bookings = bookingsDto.Select(b => new TourViet.ViewModels.BookingViewModel
        {
            BookingID = b.BookingID,
            BookingRef = b.BookingRef,
            Status = b.Status,
            StatusBadgeClass = GetStatusBadgeClass(b.Status),
            StatusIcon = GetStatusIcon(b.Status),
            CreatedAt = b.BookingDate,
            FormattedCreatedAt = b.BookingDate.ToString("dd/MM/yyyy HH:mm"),
            
            // Customer
            UserID = b.UserID,
            CustomerName = b.CustomerName,
            CustomerEmail = b.CustomerEmail,
            CustomerPhone = b.CustomerPhone,
            CustomerAddress = b.CustomerAddress,
            CustomerInitials = string.IsNullOrEmpty(b.CustomerName) ? "U" : b.CustomerName.Substring(0, 1).ToUpper(),
            
            // Tour
            TourID = b.TourID,
            TourName = b.TourName,
            TourCategory = b.TourCategory ?? "N/A",
            
            // Instance
            InstanceID = b.InstanceID,
            StartDate = b.StartDate,
            EndDate = b.EndDate,
            FormattedStartDate = b.StartDate.ToString("dd/MM/yyyy"),
            FormattedEndDate = b.EndDate.ToString("dd/MM/yyyy"),
            DurationDays = b.DurationDays,
            GuideName = b.GuideName,
            
            // Location
            LocationName = b.LocationName,
            City = b.City,
            Country = b.Country,
            
            // Pricing
            Seats = b.Seats,
            BasePrice = b.PriceBase,
            BasePriceTotal = b.PriceBase * b.Seats,
            ServicesTotal = b.ServicesTotal,
            TotalAmount = b.TotalAmount,
            Currency = b.Currency,
            FormattedTotalAmount = $"{b.TotalAmount.ToString("N0")} {b.Currency}",
            SpecialRequests = b.SpecialRequests,
            
            // Services
            BookedServices = b.BookedServices.Select(s => new TourViet.ViewModels.BookedServiceViewModel
            {
                ServiceID = s.ServiceID,
                ServiceName = s.ServiceName,
                Quantity = s.Quantity,
                PriceAtBooking = s.PriceAtBooking,
                SubTotal = s.SubTotal,
                Currency = s.Currency,
                FormattedSubTotal = $"{s.SubTotal.ToString("N0")} {s.Currency}"
            }).ToList(),
            ServicesCount = b.BookedServices.Count
        }).ToList();
        
        return View("../AdministrativeStaffPage/ManageBookings", bookings);
    }
    
    private string GetStatusBadgeClass(string status)
    {
        return status.ToLower() switch
        {
            "pending" => "bg-warning",
            "confirmed" => "bg-success",
            "cancelled" => "bg-danger",
            "completed" => "bg-info",
            _ => "bg-secondary"
        };
    }
    
    private string GetStatusIcon(string status)
    {
        return status.ToLower() switch
        {
            "pending" => "bi-clock",
            "confirmed" => "bi-check-circle",
            "cancelled" => "bi-x-circle",
            "completed" => "bi-star-fill",
            _ => "bi-circle"
        };
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

    public async Task<IActionResult> BookingHistory()
    {
        var userIdString = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return View(new List<TourViet.ViewModels.BookingViewModel>());
        }
        
        // Fetch user's bookings
        var bookingsDto = await _bookingService.GetUserBookingsAsync(userId);
        
        // Transform to ViewModel
        var bookings = bookingsDto.Select(b => new TourViet.ViewModels.BookingViewModel
        {
            BookingID = b.BookingID,
            BookingRef = b.BookingRef,
            Status = b.Status,
            StatusBadgeClass = GetStatusBadgeClass(b.Status),
            StatusIcon = GetStatusIcon(b.Status),
            CreatedAt = b.BookingDate,
            FormattedCreatedAt = b.BookingDate.ToString("dd/MM/yyyy HH:mm"),
            
            // Customer
            UserID = b.UserID,
            CustomerName = b.CustomerName,
            CustomerEmail = b.CustomerEmail,
            CustomerPhone = b.CustomerPhone,
            CustomerAddress = b.CustomerAddress,
            CustomerInitials = string.IsNullOrEmpty(b.CustomerName) ? "U" : b.CustomerName.Substring(0, 1).ToUpper(),
            
            // Tour
            TourID = b.TourID,
            TourName = b.TourName,
            TourCategory = b.TourCategory ?? "N/A",
            TourImageUrl = b.TourImageUrl,
            TourImages = b.TourImages,
            
            // Instance
            InstanceID = b.InstanceID,
            StartDate = b.StartDate,
            EndDate = b.EndDate,
            FormattedStartDate = b.StartDate.ToString("dd/MM/yyyy"),
            FormattedEndDate = b.EndDate.ToString("dd/MM/yyyy"),
            DurationDays = b.DurationDays,
            GuideName = b.GuideName,
            
            // Location
            LocationName = b.LocationName,
            City = b.City,
            Country = b.Country,
            
            // Pricing
            Seats = b.Seats,
            BasePrice = b.PriceBase,
            BasePriceTotal = b.PriceBase * b.Seats,
            ServicesTotal = b.ServicesTotal,
            TotalAmount = b.TotalAmount,
            Currency = b.Currency,
            FormattedTotalAmount = $"{b.TotalAmount.ToString("N0")} {b.Currency}",
            SpecialRequests = b.SpecialRequests,
            
            // Services
            BookedServices = b.BookedServices.Select(s => new TourViet.ViewModels.BookedServiceViewModel
            {
                ServiceID = s.ServiceID,
                ServiceName = s.ServiceName,
                Quantity = s.Quantity,
                PriceAtBooking = s.PriceAtBooking,
                SubTotal = s.SubTotal,
                Currency = s.Currency,
                FormattedSubTotal = $"{s.SubTotal.ToString("N0")} {s.Currency}"
            }).ToList(),
            ServicesCount = b.BookedServices.Count
        }).ToList();
        
        return View(bookings);
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
