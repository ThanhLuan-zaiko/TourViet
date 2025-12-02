using TourViet.Models;

namespace TourViet.Services.Interfaces;

public interface IPromotionService
{
    /// <summary>
    /// Calculates the best applicable discount for a booking context.
    /// Checks both automatic promotions and a specific coupon code if provided.
    /// </summary>
    Task<PromotionCalculationResult> CalculateDiscountAsync(
        Guid userId, 
        Guid tourId, 
        Guid instanceId, 
        decimal totalAmount, 
        int seatCount, 
        string? couponCode = null);

    /// <summary>
    /// Records the redemption of a promotion/coupon after a successful booking.
    /// </summary>
    Task RecordRedemptionAsync(
        Guid bookingId, 
        Guid userId, 
        Guid promotionId, 
        decimal discountAmount, 
        Guid? couponId = null);
}

public class PromotionCalculationResult
{
    public bool IsApplied { get; set; }
    public Guid? PromotionID { get; set; }
    public string? PromotionName { get; set; }
    public Guid? CouponID { get; set; }
    public string? CouponCode { get; set; }
    public decimal DiscountAmount { get; set; }
    public string Message { get; set; } = string.Empty;
}
