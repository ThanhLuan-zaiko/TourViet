using Microsoft.EntityFrameworkCore;
using TourViet.Data;
using TourViet.Models;
using TourViet.Services.Interfaces;

namespace TourViet.Services;

public class PromotionService : IPromotionService
{
    private readonly TourBookingDbContext _context;
    private readonly ILogger<PromotionService> _logger;

    public PromotionService(TourBookingDbContext context, ILogger<PromotionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PromotionCalculationResult> CalculateDiscountAsync(
        Guid userId, 
        Guid tourId, 
        Guid instanceId, 
        decimal totalAmount, 
        int seatCount, 
        string? couponCode = null)
    {
        var result = new PromotionCalculationResult();
        var now = DateTime.UtcNow;

        // 1. Fetch active promotions
        // We need promotions that are:
        // - Active
        // - Within date range (StartAt <= now <= EndAt)
        // - Not exhausted (Global usage limit)
        var activePromotions = await _context.Promotions
            .Include(p => p.PromotionRules)
            .Include(p => p.PromotionTargets)
            .Where(p => p.IsActive &&
                        (!p.StartAt.HasValue || p.StartAt <= now) &&
                        (!p.EndAt.HasValue || p.EndAt >= now) &&
                        (!p.MaxGlobalUses.HasValue || p.UsageCount < p.MaxGlobalUses))
            .ToListAsync();

        // 2. Filter by Target (Tour, Category, etc.)
        // We need to know the Category of the Tour to check Category targets
        var tour = await _context.Tours.FindAsync(tourId);
        if (tour == null) return result;

        var applicablePromotions = new List<Promotion>();

        foreach (var promo in activePromotions)
        {
            // Check targets
            bool isTargetMatch = false;
            if (!promo.PromotionTargets.Any() || promo.PromotionTargets.Any(t => t.TargetType == "All"))
            {
                isTargetMatch = true;
            }
            else
            {
                if (promo.PromotionTargets.Any(t => t.TargetType == "Tour" && t.TargetID == tourId)) isTargetMatch = true;
                if (promo.PromotionTargets.Any(t => t.TargetType == "Category" && t.TargetID == tour.CategoryID)) isTargetMatch = true;
            }

            if (!isTargetMatch) continue;

            // Check constraints
            if (promo.MinSeats.HasValue && seatCount < promo.MinSeats.Value) continue;
            if (promo.MinTotalAmount.HasValue && totalAmount < promo.MinTotalAmount.Value) continue;

            // Check User Usage Limit
            if (promo.MaxUsesPerUser.HasValue)
            {
                var userUsage = await _context.PromotionRedemptions
                    .CountAsync(r => r.PromotionID == promo.PromotionID && r.UserID == userId && r.Status != "Voided");
                if (userUsage >= promo.MaxUsesPerUser.Value) continue;
            }

            // Check Type & Coupon
            if (promo.PromotionType == "Automatic")
            {
                applicablePromotions.Add(promo);
            }
            else if (promo.PromotionType == "Coupon" && !string.IsNullOrEmpty(couponCode))
            {
                // Verify coupon code
                var coupon = await _context.Coupons
                    .FirstOrDefaultAsync(c => c.PromotionID == promo.PromotionID && c.Code == couponCode && c.IsActive);
                
                if (coupon != null)
                {
                    // Check coupon specific limits
                    if (coupon.ExpiresAt.HasValue && coupon.ExpiresAt < now) continue;
                    if (coupon.StartsAt.HasValue && coupon.StartsAt > now) continue;
                    if (coupon.MaxUses.HasValue && coupon.UsageCount >= coupon.MaxUses) continue;
                    
                    // Check coupon user limit
                    if (coupon.MaxUsesPerUser.HasValue)
                    {
                        var couponUserUsage = await _context.PromotionRedemptions
                            .CountAsync(r => r.CouponID == coupon.CouponID && r.UserID == userId && r.Status != "Voided");
                        if (couponUserUsage >= coupon.MaxUsesPerUser.Value) continue;
                    }

                    // Attach coupon to promo for later reference (hacky but works for local scope)
                    // Better: store tuple (Promotion, Coupon)
                    applicablePromotions.Add(promo);
                }
            }
        }

        if (!applicablePromotions.Any())
        {
            if (!string.IsNullOrEmpty(couponCode))
            {
                result.Message = "Mã giảm giá không hợp lệ hoặc không áp dụng cho tour này.";
            }
            return result;
        }

        // 3. Calculate Discount for each applicable promotion and pick the BEST one
        // (Assuming no stacking for simplicity first, or we can implement stacking if AllowStack is true)
        // For now: Pick the single highest discount value.

        decimal maxDiscount = 0;
        Promotion? bestPromo = null;
        Coupon? bestCoupon = null;

        foreach (var promo in applicablePromotions)
        {
            decimal currentPromoDiscount = 0;
            
            foreach (var rule in promo.PromotionRules)
            {
                decimal ruleDiscount = 0;
                if (rule.RuleType == "Percent")
                {
                    ruleDiscount = totalAmount * (rule.Value / 100m);
                    if (rule.MaxDiscountAmount.HasValue && ruleDiscount > rule.MaxDiscountAmount.Value)
                    {
                        ruleDiscount = rule.MaxDiscountAmount.Value;
                    }
                }
                else if (rule.RuleType == "Fixed")
                {
                    ruleDiscount = rule.Value;
                }

                currentPromoDiscount += ruleDiscount;
            }

            // Ensure discount doesn't exceed total amount
            if (currentPromoDiscount > totalAmount) currentPromoDiscount = totalAmount;

            if (currentPromoDiscount > maxDiscount)
            {
                maxDiscount = currentPromoDiscount;
                bestPromo = promo;
                
                // Find the coupon if this was a coupon promo
                if (promo.PromotionType == "Coupon" && !string.IsNullOrEmpty(couponCode))
                {
                    bestCoupon = await _context.Coupons.FirstOrDefaultAsync(c => c.PromotionID == promo.PromotionID && c.Code == couponCode);
                }
                else
                {
                    bestCoupon = null;
                }
            }
        }

        if (bestPromo != null && maxDiscount > 0)
        {
            result.IsApplied = true;
            result.PromotionID = bestPromo.PromotionID;
            result.PromotionName = bestPromo.Name;
            result.DiscountAmount = maxDiscount;
            result.CouponID = bestCoupon?.CouponID;
            result.CouponCode = bestCoupon?.Code;
            result.Message = $"Áp dụng thành công: {bestPromo.Name}";
        }
        else if (!string.IsNullOrEmpty(couponCode) && !result.IsApplied)
        {
             result.Message = "Mã giảm giá hợp lệ nhưng giá trị giảm bằng 0 (có thể do điều kiện đơn hàng).";
        }

        return result;
    }

    public async Task RecordRedemptionAsync(
        Guid bookingId, 
        Guid userId, 
        Guid promotionId, 
        decimal discountAmount, 
        Guid? couponId = null)
    {
        var redemption = new PromotionRedemption
        {
            RedemptionID = Guid.NewGuid(),
            PromotionID = promotionId,
            CouponID = couponId,
            BookingID = bookingId,
            UserID = userId,
            DiscountAmount = discountAmount,
            Currency = "VND", // Default for now
            Status = "Applied",
            AppliedAt = DateTime.UtcNow
        };

        _context.PromotionRedemptions.Add(redemption);

        // Update counts
        var promo = await _context.Promotions.FindAsync(promotionId);
        if (promo != null)
        {
            promo.UsageCount++;
        }

        if (couponId.HasValue)
        {
            var coupon = await _context.Coupons.FindAsync(couponId.Value);
            if (coupon != null)
            {
                coupon.UsageCount++;
            }
        }

        await _context.SaveChangesAsync();
    }
}
