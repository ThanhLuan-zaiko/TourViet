using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourViet.Data;
using TourViet.Models;

namespace TourViet.Controllers;

public class PromotionController : Controller
{
    private readonly TourBookingDbContext _context;

    public PromotionController(TourBookingDbContext context)
    {
        _context = context;
    }

    // GET: Promotion
    public async Task<IActionResult> Index()
    {
        var userRoles = HttpContext.Session.GetString("Roles")?.Split(',') ?? new string[0];
        var isAdministrativeStaff = userRoles.Contains("AdministrativeStaff");

        if (!isAdministrativeStaff)
        {
            return RedirectToAction("Index", "Home");
        }

        var promotions = await _context.Promotions
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return View("../AdministrativeStaffPage/Promotion/Index", promotions);
    }

    // GET: Promotion/Create
    [HttpGet]
    public IActionResult Create()
    {
        var userRoles = HttpContext.Session.GetString("Roles")?.Split(',') ?? new string[0];
        var isAdministrativeStaff = userRoles.Contains("AdministrativeStaff");

        if (!isAdministrativeStaff)
        {
            return RedirectToAction("Index", "Home");
        }

        var promotion = new Promotion
        {
            Name = string.Empty,
            PromotionType = string.Empty,
            IsActive = true,
            Priority = 100,
            StartAt = DateTime.UtcNow,
            EndAt = DateTime.UtcNow.AddMonths(1)
        };

        return View("../AdministrativeStaffPage/Promotion/Create", promotion);
    }

    // POST: Promotion/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Promotion promotion)
    {
        var userRoles = HttpContext.Session.GetString("Roles")?.Split(',') ?? new string[0];
        var isAdministrativeStaff = userRoles.Contains("AdministrativeStaff");

        if (!isAdministrativeStaff)
        {
            return RedirectToAction("Index", "Home");
        }

        if (ModelState.IsValid)
        {
            promotion.PromotionID = Guid.NewGuid();
            promotion.CreatedAt = DateTime.UtcNow;
            
            // Generate slug from name if not provided
            if (string.IsNullOrWhiteSpace(promotion.Slug))
            {
                promotion.Slug = GenerateSlug(promotion.Name);
            }

            _context.Promotions.Add(promotion);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Chương trình khuyến mãi đã được tạo thành công!";
            return RedirectToAction(nameof(Index));
        }

        return View("../AdministrativeStaffPage/Promotion/Create", promotion);
    }

    // GET: Promotion/Edit/5
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var userRoles = HttpContext.Session.GetString("Roles")?.Split(',') ?? new string[0];
        var isAdministrativeStaff = userRoles.Contains("AdministrativeStaff");

        if (!isAdministrativeStaff)
        {
            return RedirectToAction("Index", "Home");
        }

        var promotion = await _context.Promotions
            .Include(p => p.PromotionRules)
            .Include(p => p.PromotionTargets)
            .Include(p => p.Coupons)
            .FirstOrDefaultAsync(p => p.PromotionID == id);

        if (promotion == null)
        {
            return NotFound();
        }

        return View("../AdministrativeStaffPage/Promotion/Edit", promotion);
    }

    // POST: Promotion/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Promotion promotion)
    {
        var userRoles = HttpContext.Session.GetString("Roles")?.Split(',') ?? new string[0];
        var isAdministrativeStaff = userRoles.Contains("AdministrativeStaff");

        if (!isAdministrativeStaff)
        {
            return RedirectToAction("Index", "Home");
        }

        if (ModelState.IsValid)
        {
            var existingPromotion = await _context.Promotions
                .FirstOrDefaultAsync(p => p.PromotionID == promotion.PromotionID);

            if (existingPromotion == null)
            {
                return NotFound();
            }

            existingPromotion.Name = promotion.Name;
            existingPromotion.Slug = promotion.Slug;
            existingPromotion.Description = promotion.Description;
            existingPromotion.PromotionType = promotion.PromotionType;
            existingPromotion.StartAt = promotion.StartAt;
            existingPromotion.EndAt = promotion.EndAt;
            existingPromotion.IsActive = promotion.IsActive;
            existingPromotion.Priority = promotion.Priority;
            existingPromotion.AllowStack = promotion.AllowStack;
            existingPromotion.MaxGlobalUses = promotion.MaxGlobalUses;
            existingPromotion.MaxUsesPerUser = promotion.MaxUsesPerUser;
            existingPromotion.MinTotalAmount = promotion.MinTotalAmount;
            existingPromotion.MinSeats = promotion.MinSeats;
            existingPromotion.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Chương trình khuyến mãi đã được cập nhật thành công!";
            return RedirectToAction(nameof(Index));
        }

        return View("../AdministrativeStaffPage/Promotion/Edit", promotion);
    }

    // GET: Promotion/Delete/5
    [HttpGet]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userRoles = HttpContext.Session.GetString("Roles")?.Split(',') ?? new string[0];
        var isAdministrativeStaff = userRoles.Contains("AdministrativeStaff");

        if (!isAdministrativeStaff)
        {
            return RedirectToAction("Index", "Home");
        }

        var promotion = await _context.Promotions
            .Include(p => p.PromotionRules)
            .Include(p => p.PromotionTargets)
            .Include(p => p.Coupons)
            .FirstOrDefaultAsync(p => p.PromotionID == id);

        if (promotion == null)
        {
            return NotFound();
        }

        return View("../AdministrativeStaffPage/Promotion/Delete", promotion);
    }

    // POST: Promotion/DeleteConfirmed
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed([FromForm] Guid id)
    {
        var userRoles = HttpContext.Session.GetString("Roles")?.Split(',') ?? new string[0];
        var isAdministrativeStaff = userRoles.Contains("AdministrativeStaff");

        if (!isAdministrativeStaff)
        {
            return Json(new { success = false, message = "Bạn không có quyền thực hiện hành động này." });
        }

        try
        {
            var rowsAffected = await _context.Promotions
                .Where(p => p.PromotionID == id)
                .ExecuteDeleteAsync();

            if (rowsAffected == 0)
            {
                return Json(new { success = false, message = "Chương trình khuyến mãi không tồn tại hoặc đã bị xóa trước đó." });
            }

            return Json(new { success = true, message = "Chương trình khuyến mãi đã được xóa thành công!" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting promotion: {ex.Message}");
            return Json(new { success = false, message = "Có lỗi xảy ra khi xóa chương trình khuyến mãi." });
        }
    }

    // Helper method to generate slug from name
    private string GenerateSlug(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        // Convert to lowercase and replace spaces with hyphens
        var slug = name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("đ", "d")
            .Replace("á", "a")
            .Replace("à", "a")
            .Replace("ả", "a")
            .Replace("ã", "a")
            .Replace("ạ", "a")
            .Replace("ă", "a")
            .Replace("ắ", "a")
            .Replace("ằ", "a")
            .Replace("ẳ", "a")
            .Replace("ẵ", "a")
            .Replace("ặ", "a")
            .Replace("â", "a")
            .Replace("ấ", "a")
            .Replace("ầ", "a")
            .Replace("ẩ", "a")
            .Replace("ẫ", "a")
            .Replace("ậ", "a")
            .Replace("é", "e")
            .Replace("è", "e")
            .Replace("ẻ", "e")
            .Replace("ẽ", "e")
            .Replace("ẹ", "e")
            .Replace("ê", "e")
            .Replace("ế", "e")
            .Replace("ề", "e")
            .Replace("ể", "e")
            .Replace("ễ", "e")
            .Replace("ệ", "e")
            .Replace("í", "i")
            .Replace("ì", "i")
            .Replace("ỉ", "i")
            .Replace("ĩ", "i")
            .Replace("ị", "i")
            .Replace("ó", "o")
            .Replace("ò", "o")
            .Replace("ỏ", "o")
            .Replace("õ", "o")
            .Replace("ọ", "o")
            .Replace("ô", "o")
            .Replace("ố", "o")
            .Replace("ồ", "o")
            .Replace("ổ", "o")
            .Replace("ỗ", "o")
            .Replace("ộ", "o")
            .Replace("ơ", "o")
            .Replace("ớ", "o")
            .Replace("ờ", "o")
            .Replace("ở", "o")
            .Replace("ỡ", "o")
            .Replace("ợ", "o")
            .Replace("ú", "u")
            .Replace("ù", "u")
            .Replace("ủ", "u")
            .Replace("ũ", "u")
            .Replace("ụ", "u")
            .Replace("ư", "u")
            .Replace("ứ", "u")
            .Replace("ừ", "u")
            .Replace("ử", "u")
            .Replace("ữ", "u")
            .Replace("ự", "u")
            .Replace("ý", "y")
            .Replace("ỳ", "y")
            .Replace("ỷ", "y")
            .Replace("ỹ", "y")
            .Replace("ỵ", "y");

        // Remove any non-alphanumeric characters except hyphens
        slug = new string(slug.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());

        return slug;
    }
}
