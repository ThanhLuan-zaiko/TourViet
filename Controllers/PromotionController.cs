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
            .Include(p => p.PromotionRedemptions)
                .ThenInclude(r => r.User)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        // Calculate dashboard statistics
        var allRedemptions = promotions.SelectMany(p => p.PromotionRedemptions).ToList();
        
        var viewModel = new ViewModels.PromotionDashboardViewModel
        {
            Promotions = promotions,
            TotalPromotions = promotions.Count,
            ActivePromotions = promotions.Count(p => p.IsActive),
            TotalRedemptions = allRedemptions.Count,
            TotalDiscountAmount = allRedemptions.Sum(r => r.DiscountAmount),
            AverageDiscountPerRedemption = allRedemptions.Any() ? allRedemptions.Average(r => r.DiscountAmount) : 0,
            
            // Top 5 promotions by redemption count
            TopPromotions = promotions
                .Select(p => new ViewModels.PromotionStatItem
                {
                    PromotionID = p.PromotionID,
                    Name = p.Name,
                    RedemptionCount = p.PromotionRedemptions.Count,
                    TotalDiscount = p.PromotionRedemptions.Sum(r => r.DiscountAmount),
                    Currency = p.PromotionRedemptions.FirstOrDefault()?.Currency ?? "VND"
                })
                .OrderByDescending(p => p.RedemptionCount)
                .Take(5)
                .ToList(),
                
            // Recent 10 redemptions
            RecentRedemptions = allRedemptions
                .OrderByDescending(r => r.AppliedAt)
                .Take(10)
                .Select(r => new ViewModels.RecentRedemptionItem
                {
                    PromotionName = r.Promotion?.Name ?? "Unknown",
                    UserName = r.User?.FullName ?? "Guest",
                    DiscountAmount = r.DiscountAmount,
                    Currency = r.Currency,
                    AppliedAt = r.AppliedAt,
                    Status = r.Status
                })
                .ToList()
        };

        return View("../AdministrativeStaffPage/Promotion/Index", viewModel);
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
            .Include(p => p.PromotionRedemptions)
                .ThenInclude(r => r.User)
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

    #region Promotion Rules Management

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddRule([FromForm] Guid promotionId, [FromForm] string ruleType, [FromForm] decimal value, [FromForm] string? conditions)
    {
        var userRoles = HttpContext.Session.GetString("Roles")?.Split(',') ?? new string[0];
        if (!userRoles.Contains("AdministrativeStaff")) return Json(new { success = false, message = "Unauthorized" });

        var promotion = await _context.Promotions.FindAsync(promotionId);
        if (promotion == null) return Json(new { success = false, message = "Promotion not found" });

        var rule = new PromotionRule
        {
            PromotionID = promotionId,
            RuleType = ruleType,
            Value = value,
            Conditions = conditions,
            Promotion = promotion
        };

        _context.PromotionRules.Add(rule);
        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Rule added successfully" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRule([FromForm] Guid id)
    {
        var userRoles = HttpContext.Session.GetString("Roles")?.Split(',') ?? new string[0];
        if (!userRoles.Contains("AdministrativeStaff")) return Json(new { success = false, message = "Unauthorized" });

        var rule = await _context.PromotionRules.FindAsync(id);
        if (rule == null) return Json(new { success = false, message = "Rule not found" });

        _context.PromotionRules.Remove(rule);
        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Rule deleted successfully" });
    }

    #endregion

    #region Promotion Targets Management

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTarget([FromForm] Guid promotionId, [FromForm] string targetType, [FromForm] Guid? targetId)
    {
        var userRoles = HttpContext.Session.GetString("Roles")?.Split(',') ?? new string[0];
        if (!userRoles.Contains("AdministrativeStaff")) return Json(new { success = false, message = "Unauthorized" });

        var promotion = await _context.Promotions.FindAsync(promotionId);
        if (promotion == null) return Json(new { success = false, message = "Promotion not found" });

        var target = new PromotionTarget
        {
            PromotionID = promotionId,
            TargetType = targetType,
            TargetID = targetId,
            Promotion = promotion
        };

        _context.PromotionTargets.Add(target);
        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Target added successfully" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTarget([FromForm] Guid id)
    {
        var userRoles = HttpContext.Session.GetString("Roles")?.Split(',') ?? new string[0];
        if (!userRoles.Contains("AdministrativeStaff")) return Json(new { success = false, message = "Unauthorized" });

        var target = await _context.PromotionTargets.FindAsync(id);
        if (target == null) return Json(new { success = false, message = "Target not found" });

        _context.PromotionTargets.Remove(target);
        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Target deleted successfully" });
    }

    #endregion

    #region Coupons Management

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateCoupons([FromForm] Guid promotionId, [FromForm] int quantity, [FromForm] string prefix, [FromForm] int length)
    {
        var userRoles = HttpContext.Session.GetString("Roles")?.Split(',') ?? new string[0];
        if (!userRoles.Contains("AdministrativeStaff")) return Json(new { success = false, message = "Unauthorized" });

        var promotion = await _context.Promotions.FindAsync(promotionId);
        if (promotion == null) return Json(new { success = false, message = "Promotion not found" });

        if (quantity <= 0 || quantity > 1000) return Json(new { success = false, message = "Quantity must be between 1 and 1000" });
        if (length < 4 || length > 20) return Json(new { success = false, message = "Length must be between 4 and 20" });

        var coupons = new List<Coupon>();
        var random = new Random();
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        for (int i = 0; i < quantity; i++)
        {
            var code = prefix + new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
            
            // Ensure uniqueness (simple check, in production might need DB check loop)
            if (!await _context.Coupons.AnyAsync(c => c.Code == code))
            {
                coupons.Add(new Coupon
                {
                    PromotionID = promotionId,
                    Code = code,
                    IsActive = true,
                    Promotion = promotion
                });
            }
        }

        _context.Coupons.AddRange(coupons);
        await _context.SaveChangesAsync();

        return Json(new { success = true, message = $"{coupons.Count} coupons generated successfully" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCoupon([FromForm] Guid id)
    {
        var userRoles = HttpContext.Session.GetString("Roles")?.Split(',') ?? new string[0];
        if (!userRoles.Contains("AdministrativeStaff")) return Json(new { success = false, message = "Unauthorized" });

        var coupon = await _context.Coupons.FindAsync(id);
        if (coupon == null) return Json(new { success = false, message = "Coupon not found" });

        _context.Coupons.Remove(coupon);
        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Coupon deleted successfully" });
    }
    
    [HttpGet]
    public async Task<IActionResult> ExportCoupons(Guid promotionId)
    {
        var userRoles = HttpContext.Session.GetString("Roles")?.Split(',') ?? new string[0];
        if (!userRoles.Contains("AdministrativeStaff")) return Unauthorized();

        var promotion = await _context.Promotions
            .Include(p => p.Coupons)
            .FirstOrDefaultAsync(p => p.PromotionID == promotionId);

        if (promotion == null) return NotFound();

        // Generate CSV
        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Mã giảm giá,Trạng thái,Ngày tạo,Ngày bắt đầu,Ngày hết hạn,Số lần sử dụng tối đa,Số lần đã dùng,Mô tả");

        foreach (var coupon in promotion.Coupons)
        {
            csv.AppendLine($"\"{coupon.Code}\",\"{(coupon.IsActive ? "Hoạt động" : "Vô hiệu")}\",\"{coupon.CreatedAt:dd/MM/yyyy HH:mm}\",\"{coupon.StartsAt?.ToString("dd/MM/yyyy HH:mm") ?? "N/A"}\",\"{coupon.ExpiresAt?.ToString("dd/MM/yyyy HH:mm") ?? "N/A"}\",\"{coupon.MaxUses?.ToString() ?? "Không giới hạn"}\",\"{coupon.UsageCount}\",\"{coupon.Description ?? ""}\"");
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
        var fileName = $"Coupons_{promotion.Slug ?? promotion.Name}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

        return File(bytes, "text/csv", fileName);
    }

    #endregion
}
