using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourViet.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace TourViet.Controllers
{
    /// <summary>
    /// Public-facing promotions controller for customers
    /// NOTE: This is different from PromotionController (admin)
    /// </summary>
    public class PromotionsController : Controller
    {
        private readonly TourBookingDbContext _context;

        public PromotionsController(TourBookingDbContext context)
        {
            _context = context;
        }

        // GET: /Promotions
        public async Task<IActionResult> Index(string type = "")
        {
            var now = DateTime.UtcNow;
            var userId = HttpContext.Session.GetString("UserId");
            Guid? userGuid = string.IsNullOrEmpty(userId) ? null : Guid.Parse(userId);
            
            var query = _context.Promotions
                .Include(p => p.PromotionRules)
                .Include(p => p.PromotionTargets)
                .Include(p => p.Coupons)
                .Where(p => p.IsActive 
                    && (!p.StartAt.HasValue || p.StartAt <= now)
                    && (!p.EndAt.HasValue || p.EndAt >= now));

            // Filter out promotions already used by this user (if logged in)
            if (userGuid.HasValue)
            {
                var usedPromotionIds = await _context.PromotionRedemptions
                    .Where(r => r.UserID == userGuid.Value && r.Status == "Confirmed")
                    .Select(r => r.PromotionID)
                    .Distinct()
                    .ToListAsync();
                
                query = query.Where(p => !usedPromotionIds.Contains(p.PromotionID));
            }

            // Filter by type if specified
            if (!string.IsNullOrEmpty(type))
            {
                query = query.Where(p => p.PromotionType == type);
            }

            var promotions = await query
                .OrderByDescending(p => p.Priority)
                .ThenByDescending(p => p.CreatedAt)
                .ToListAsync();

            ViewBag.SelectedType = type;
            return View(promotions);
        }

        // GET: /Promotions/Details/slug-or-id
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var now = DateTime.UtcNow;

            // Try to find by slug first, then by ID
            var promotion = await _context.Promotions
                .Include(p => p.PromotionRules)
                .Include(p => p.PromotionTargets)
                    .ThenInclude(t => t.Tour)
                .Include(p => p.Coupons.Where(c => c.IsActive))
                .Where(p => p.IsActive 
                    && (!p.StartAt.HasValue || p.StartAt <= now)
                    && (!p.EndAt.HasValue || p.EndAt >= now))
                .FirstOrDefaultAsync(p => p.Slug == id || p.PromotionID.ToString() == id);

            if (promotion == null)
                return NotFound();

            // Check if logged-in user already used this promotion
            var userId = HttpContext.Session.GetString("UserId");
            if (!string.IsNullOrEmpty(userId))
            {
                var userGuid = Guid.Parse(userId);
                var hasUsed = await _context.PromotionRedemptions
                    .AnyAsync(r => r.PromotionID == promotion.PromotionID 
                                && r.UserID == userGuid 
                                && r.Status == "Confirmed");
                
                ViewBag.AlreadyUsed = hasUsed;
            }
            else
            {
                ViewBag.AlreadyUsed = false;
            }

            return View(promotion);
        }

        // GET: /Promotions/GetCoupon/{id}
        // Returns a coupon code for display (no authentication required)
        public async Task<IActionResult> GetCoupon(Guid id)
        {
            var coupon = await _context.Coupons
                .Include(c => c.Promotion)
                .FirstOrDefaultAsync(c => c.CouponID == id && c.IsActive);

            if (coupon == null || !coupon.Promotion.IsActive)
                return Json(new { success = false, message = "Mã giảm giá không tồn tại hoặc đã hết hạn" });

            return Json(new { success = true, code = coupon.Code, description = coupon.Description });
        }
    }
}
