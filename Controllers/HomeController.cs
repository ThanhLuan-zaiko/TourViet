using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
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
    private readonly ICustomerService _customerService;
    private readonly IHubContext<TourViet.Hubs.UserHub> _userHubContext;

    public HomeController(
        ILogger<HomeController> logger, 
        TourBookingDbContext context,
        ITourService tourService,
        IBookingService bookingService,
        ICustomerService customerService,
        IHubContext<TourViet.Hubs.UserHub> userHubContext)
    {
        _logger = logger;
        _context = context;
        _tourService = tourService;
        _bookingService = bookingService;
        _customerService = customerService;
        _userHubContext = userHubContext;
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

        // Find applicable promotions for this tour
        var now = DateTime.UtcNow;
        var applicablePromotions = await _context.Promotions
            .Include(p => p.PromotionRules)
            .Include(p => p.PromotionTargets)
            .Where(p => p.IsActive 
                && (!p.StartAt.HasValue || p.StartAt <= now)
                && (!p.EndAt.HasValue || p.EndAt >= now)
                && (p.PromotionType == "Automatic" || p.PromotionType == "FlashSale") // Only show auto-apply promotions
                && (p.PromotionTargets.Any(t => t.TargetType == "All") || 
                    p.PromotionTargets.Any(t => t.TargetType == "Tour" && t.TargetID == id)))
            .OrderByDescending(p => p.Priority)
            .ToListAsync();

        ViewBag.ApplicablePromotions = applicablePromotions;

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
        
        // Get all promotion redemptions for these bookings
        var bookingIds = bookingsDto.Select(b => b.BookingID).ToList();
        var promotionRedemptions = await _context.PromotionRedemptions
            .Include(r => r.Promotion)
            .Include(r => r.Coupon)
            .Where(r => r.BookingID.HasValue && bookingIds.Contains(r.BookingID.Value))
            .ToListAsync();
        
        // Transform to ViewModel
        var bookings = bookingsDto.Select(b =>
        {
            var redemption = promotionRedemptions.FirstOrDefault(r => r.BookingID == b.BookingID);
            var subTotalBeforeDiscount = b.PriceBase * b.Seats + b.ServicesTotal;
            var discountAmount = redemption?.DiscountAmount ?? 0;
            
            return new TourViet.ViewModels.BookingViewModel
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
                SubTotalBeforeDiscount = subTotalBeforeDiscount,
                FormattedSubTotalBeforeDiscount = $"{subTotalBeforeDiscount.ToString("N0")} {b.Currency}",
                TotalAmount = b.TotalAmount,
                Currency = b.Currency,
                FormattedTotalAmount = $"{b.TotalAmount.ToString("N0")} {b.Currency}",
                SpecialRequests = b.SpecialRequests,
                
                // Promotion Info
                PromotionName = redemption?.Promotion?.Name,
                DiscountAmount = discountAmount,
                FormattedDiscountAmount = discountAmount > 0 ? $"-{discountAmount.ToString("N0")} {b.Currency}" : null,
                CouponCode = redemption?.Coupon?.Code,
                
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
            };
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

    public async Task<IActionResult> TourSchedule(int? month, int? year)
    {
        var userRoles = HttpContext.Session.GetString("Roles")?.Split(',') ?? new string[0];
        var isAdministrativeStaff = userRoles.Contains("AdministrativeStaff");
        
        if (!isAdministrativeStaff)
        {
            return RedirectToAction("Index");
        }

        var now = DateTime.UtcNow;
        var selectedMonth = month ?? now.Month;
        var selectedYear = year ?? now.Year;
        
        // Month view data
        var monthStart = new DateTime(selectedYear, selectedMonth, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var monthInstances = await _tourService.GetTourInstancesAsync(monthStart, monthEnd);

        // Week view data (current week)
        var today = now.Date;
        var currentDayOfWeek = (int)today.DayOfWeek;
        var weekStart = today.AddDays(-currentDayOfWeek + 1); // Monday
        var weekEnd = weekStart.AddDays(6); // Sunday
        var weekInstances = await _tourService.GetTourInstancesAsync(weekStart, weekEnd);

        ViewBag.SelectedMonth = selectedMonth;
        ViewBag.SelectedYear = selectedYear;
        ViewBag.WeekInstances = weekInstances;
        
        return View("../AdministrativeStaffPage/TourSchedule", monthInstances);
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
        
        // Get promotion redemptions for these bookings
        var bookingIds = bookingsDto.Select(b => b.BookingID).ToList();
        var promotionRedemptions = await _context.PromotionRedemptions
            .Include(r => r.Promotion)
            .Include(r => r.Coupon)
            .Where(r => r.BookingID.HasValue && bookingIds.Contains(r.BookingID.Value))
            .ToListAsync();
        
        // Transform to ViewModel
        var bookings = bookingsDto.Select(b =>
        {
            var redemption = promotionRedemptions.FirstOrDefault(r => r.BookingID == b.BookingID);
            var subTotalBeforeDiscount = b.PriceBase * b.Seats + b.ServicesTotal;
            var discountAmount = redemption?.DiscountAmount ?? 0;
            
            return new TourViet.ViewModels.BookingViewModel
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
                SubTotalBeforeDiscount = subTotalBeforeDiscount,
                FormattedSubTotalBeforeDiscount = $"{subTotalBeforeDiscount.ToString("N0")} {b.Currency}",
                TotalAmount = b.TotalAmount,
                Currency = b.Currency,
                FormattedTotalAmount = $"{b.TotalAmount.ToString("N0")} {b.Currency}",
                SpecialRequests = b.SpecialRequests,
                
                // Promotion Info
                PromotionName = redemption?.Promotion?.Name,
                DiscountAmount = discountAmount,
                FormattedDiscountAmount = discountAmount > 0 ? $"-{discountAmount.ToString("N0")} {b.Currency}" : null,
                CouponCode = redemption?.Coupon?.Code,
                
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
            };
        }).ToList();
        
        return View(bookings);
    }

    public async Task<IActionResult> TrackItinerary()
    {
        var userIdString = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return View(new List<TourViet.ViewModels.TourItineraryViewModel>());
        }
        
        // Fetch user's bookings
        var bookingsDto = await _bookingService.GetUserBookingsAsync(userId);
        
        // Filter only Confirmed and Completed bookings
        var confirmedBookings = bookingsDto
            .Where(b => b.Status == "Confirmed" || b.Status == "Completed")
            .ToList();
        
        // Get TourIDs to fetch itineraries and location info
        var tourIds = confirmedBookings.Select(b => b.TourID).Distinct().ToList();
        
        // Fetch itineraries for these tours
        var itineraries = await _context.Itineraries
            .Where(i => tourIds.Contains(i.TourID))
            .OrderBy(i => i.TourID)
            .ThenBy(i => i.DayIndex)
            .ToListAsync();

        // Fetch location info for these tours
        var toursLocation = await _context.Tours
            .Include(t => t.Location)
            .Where(t => tourIds.Contains(t.TourID))
            .ToDictionaryAsync(t => t.TourID, t => t.Location);
        
        // Transform to ViewModel
        var tourItineraries = confirmedBookings.Select(b =>
        {
            // Get itineraries for this specific tour
            var tourItineraryItems = itineraries
                .Where(i => i.TourID == b.TourID)
                .Select(i => new TourViet.ViewModels.ItineraryItemViewModel
                {
                    ItineraryID = i.ItineraryID,
                    DayIndex = i.DayIndex,
                    Title = i.Title,
                    Description = i.Description
                })
                .ToList();
            
            // Get location info
            var location = toursLocation.ContainsKey(b.TourID) ? toursLocation[b.TourID] : null;

            return new TourViet.ViewModels.TourItineraryViewModel
            {
                BookingID = b.BookingID,
                BookingRef = b.BookingRef,
                Status = b.Status,
                StatusBadgeClass = GetStatusBadgeClass(b.Status),
                StatusIcon = GetStatusIcon(b.Status),
                CreatedAt = b.BookingDate,
                FormattedCreatedAt = b.BookingDate.ToString("dd/MM/yyyy HH:mm"),
                
                // Customer
                CustomerName = b.CustomerName,
                CustomerEmail = b.CustomerEmail,
                CustomerPhone = b.CustomerPhone,
                
                // Tour
                TourID = b.TourID,
                TourName = b.TourName,
                TourCategory = b.TourCategory ?? "N/A",
                TourImageUrl = b.TourImageUrl,
                TourImages = b.TourImages,
                
                // Instance
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
                DestinationLat = location?.Latitude,
                DestinationLng = location?.Longitude,
                DestinationAddress = location?.Address,
                
                // Booking Details
                Seats = b.Seats,
                TotalAmount = b.TotalAmount,
                Currency = b.Currency,
                FormattedTotalAmount = $"{b.TotalAmount.ToString("N0")} {b.Currency}",
                
                // Itineraries
                Itineraries = tourItineraryItems
            };
        }).ToList();
        
        return View(tourItineraries);
    }


    public async Task<IActionResult> CustomerList()
    {
        var userRoles = HttpContext.Session.GetString("Roles")?.Split(',') ?? new string[0];
        var isAdministrativeStaff = userRoles.Contains("AdministrativeStaff");
        
        if (!isAdministrativeStaff)
        {
            return RedirectToAction("Index");
        }
        
        // Get all customers - DataTables will handle pagination client-side
        var customers = await _customerService.GetCustomersPagedAsync(1, int.MaxValue);
        var totalCustomers = customers.Count;
        
        // Calculate statistics
        ViewBag.TotalCustomers = totalCustomers;
        ViewBag.CustomersThisMonth = customers.Count(c => c.CreatedAt >= DateTime.UtcNow.AddMonths(-1));
        ViewBag.TotalBookings = customers.Sum(c => c.TotalBookings);
        ViewBag.TotalRevenue = customers.Sum(c => c.TotalSpent);
        
        return View("../AdministrativeStaffPage/CustomerList", customers);
    }
    
    public async Task<IActionResult> CustomerDetails(Guid? id)
    {
        var userRoles = HttpContext.Session.GetString("Roles")?.Split(',') ?? new string[0];
        var isAdministrativeStaff = userRoles.Contains("AdministrativeStaff");
        
        if (!isAdministrativeStaff)
        {
            return RedirectToAction("Index");
        }
        
        if (id == null)
        {
            return NotFound();
        }
        
        var customerDetails = await _customerService.GetCustomerDetailsAsync(id.Value);
        
        if (customerDetails == null)
        {
            return NotFound();
        }
        
        return View("../AdministrativeStaffPage/CustomerDetails", customerDetails);
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BanCustomer(Guid id)
    {
        var userRoles = HttpContext.Session.GetString("Roles")?.Split(',') ?? new string[0];
        var isAdministrativeStaff = userRoles.Contains("AdministrativeStaff");
        
        if (!isAdministrativeStaff)
        {
            return Json(new { success = false, message = "Bạn không có quyền thực hiện hành động này." });
        }
        
        try
        {
            var result = await _customerService.BanCustomerAsync(id);
            
            if (result.Success)
            {
                // Broadcast ban event via SignalR to logout user immediately
                await _userHubContext.Clients.Group($"user_{id}").SendAsync("ForceLogout", "Tài khoản của bạn đã bị cấm. Vui lòng liên hệ Admin.");
                _logger.LogInformation("Customer {CustomerId} banned successfully and logout signal sent", id);
            }
            
            return Json(new { success = result.Success, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error banning customer {CustomerId}", id);
            return Json(new { success = false, message = "Có lỗi xảy ra khi cấm tài khoản." });
        }
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnbanCustomer(Guid id)
    {
        var userRoles = HttpContext.Session.GetString("Roles")?.Split(',') ?? new string[0];
        var isAdministrativeStaff = userRoles.Contains("AdministrativeStaff");
        
        if (!isAdministrativeStaff)
        {
            return Json(new { success = false, message = "Bạn không có quyền thực hiện hành động này." });
        }
        
        try
        {
            var result = await _customerService.UnbanCustomerAsync(id);
            
            return Json(new { success = result.Success, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unbanning customer {CustomerId}", id);
            return Json(new { success = false, message = "Có lỗi xảy ra khi bỏ cấm tài khoản." });
        }
    }
}
