using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourViet.Data;
using TourViet.Models;
using TourViet.Services;

namespace TourViet.Controllers;

public class AdminController : Controller
{
    private readonly TourBookingDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public AdminController(TourBookingDbContext context, IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }
    
    // Service Management Actions for AdministrativeStaff
    public async Task<IActionResult> ManageServices()
    {
        var userRoles = HttpContext.Session.GetString("Roles")?.Split(',') ?? new string[0];
        var isAdministrativeStaff = userRoles.Contains("AdministrativeStaff");
        
        if (!isAdministrativeStaff)
        {
            return RedirectToAction("Index", "Home");
        }
        
        var services = await _context.Services
            .Where(s => !s.IsDeleted)
            .OrderBy(s => s.ServiceName)
            .ToListAsync();
        
        return View("../AdministrativeStaffPage/ManageServices", services);
    }

    [HttpGet]
    public async Task<IActionResult> CreateService()
    {
        var userRoles = HttpContext.Session.GetString("Roles")?.Split(',') ?? new string[0];
        var isAdministrativeStaff = userRoles.Contains("AdministrativeStaff");
        
        if (!isAdministrativeStaff)
        {
            return RedirectToAction("Index", "Home");
        }
        
        await Task.Yield(); // To avoid CS1998 warning
        return View("../AdministrativeStaffPage/CreateService");
    }

    [HttpPost]
    public async Task<IActionResult> CreateService(Service service)
    {
        var userRoles = HttpContext.Session.GetString("Roles")?.Split(',') ?? new string[0];
        var isAdministrativeStaff = userRoles.Contains("AdministrativeStaff");
        
        if (!isAdministrativeStaff)
        {
            return RedirectToAction("Index", "Home");
        }
        
        if (ModelState.IsValid)
        {
            service.ServiceID = Guid.NewGuid();
            service.CreatedAt = DateTime.UtcNow;
            service.IsDeleted = false;
            
            _context.Services.Add(service);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Dịch vụ đã được tạo thành công!";
            return RedirectToAction("ManageServices");
        }
        
        await Task.Yield(); // To avoid CS1998 warning
        return View("../AdministrativeStaffPage/CreateService", service);
    }

    [HttpGet]
    public async Task<IActionResult> EditService(Guid id)
    {
        var userRoles = HttpContext.Session.GetString("Roles")?.Split(',') ?? new string[0];
        var isAdministrativeStaff = userRoles.Contains("AdministrativeStaff");
        
        if (!isAdministrativeStaff)
        {
            return RedirectToAction("Index", "Home");
        }
        
        var service = await _context.Services
            .FirstOrDefaultAsync(s => s.ServiceID == id && !s.IsDeleted);
        
        if (service == null)
        {
            return NotFound();
        }
        
        await Task.Yield(); // To avoid CS1998 warning
        return View("../AdministrativeStaffPage/EditService", service);
    }

    [HttpPost]
    public async Task<IActionResult> EditService(Service service)
    {
        var userRoles = HttpContext.Session.GetString("Roles")?.Split(',') ?? new string[0];
        var isAdministrativeStaff = userRoles.Contains("AdministrativeStaff");
        
        if (!isAdministrativeStaff)
        {
            return RedirectToAction("Index", "Home");
        }
        
        if (ModelState.IsValid)
        {
            var existingService = await _context.Services
                .FirstOrDefaultAsync(s => s.ServiceID == service.ServiceID && !s.IsDeleted);
            
            if (existingService == null)
            {
                return NotFound();
            }
            
            existingService.ServiceName = service.ServiceName;
            existingService.Code = service.Code;
            existingService.Description = service.Description;
            existingService.Price = service.Price;
            existingService.Currency = service.Currency;
            existingService.IsActive = service.IsActive;
            existingService.IsTaxable = service.IsTaxable;
            existingService.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Dịch vụ đã được cập nhật thành công!";
            return RedirectToAction("ManageServices");
        }
        
        await Task.Yield(); // To avoid CS1998 warning
        return View("../AdministrativeStaffPage/EditService", service);
    }

    [HttpGet]
    public async Task<IActionResult> DeleteService(Guid id)
    {
        var userRoles = HttpContext.Session.GetString("Roles")?.Split(',') ?? new string[0];
        var isAdministrativeStaff = userRoles.Contains("AdministrativeStaff");
        
        if (!isAdministrativeStaff)
        {
            return RedirectToAction("Index", "Home");
        }
        
        var service = await _context.Services
            .FirstOrDefaultAsync(s => s.ServiceID == id && !s.IsDeleted);
        
        if (service == null)
        {
            return NotFound();
        }
        
        await Task.Yield(); // To avoid CS1998 warning
        return View("../AdministrativeStaffPage/DeleteService", service);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteServiceConfirmed([FromForm] Guid id)
    {
        var userRoles = HttpContext.Session.GetString("Roles")?.Split(',') ?? new string[0];
        var isAdministrativeStaff = userRoles.Contains("AdministrativeStaff");
        
        if (!isAdministrativeStaff)
        {
            return Json(new { success = false, message = "Bạn không có quyền thực hiện hành động này." });
        }
        
        var service = await _context.Services
            .FirstOrDefaultAsync(s => s.ServiceID == id);
        
        if (service == null)
        {
            return Json(new { success = false, message = "Dịch vụ không tồn tại." });
        }
        
        // Kiểm tra nếu dịch vụ đã bị xóa trước đó
        if (service.IsDeleted)
        {
            return Json(new { success = false, message = "Dịch vụ đã bị xóa trước đó." });
        }
        
        service.IsDeleted = true;
        service.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        return Json(new { success = true, message = "Dịch vụ đã được xóa thành công!" });
    }
}