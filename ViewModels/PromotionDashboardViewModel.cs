using System;
using System.Collections.Generic;

namespace TourViet.ViewModels
{
    public class PromotionDashboardViewModel
    {
        public IEnumerable<TourViet.Models.Promotion> Promotions { get; set; } = new List<TourViet.Models.Promotion>();
        
        // Overall Statistics
        public int TotalPromotions { get; set; }
        public int ActivePromotions { get; set; }
        public int TotalRedemptions { get; set; }
        public decimal TotalDiscountAmount { get; set; }
        public decimal AverageDiscountPerRedemption { get; set; }
        
        // Top Performing Promotions
        public List<PromotionStatItem> TopPromotions { get; set; } = new();
        
        // Recent Redemptions
        public List<RecentRedemptionItem> RecentRedemptions { get; set; } = new();
    }
    
    public class PromotionStatItem
    {
        public Guid PromotionID { get; set; }
        public string Name { get; set; } = string.Empty;
        public int RedemptionCount { get; set; }
        public decimal TotalDiscount { get; set; }
        public string Currency { get; set; } = "VND";
    }
    
    public class RecentRedemptionItem
    {
        public string PromotionName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public decimal DiscountAmount { get; set; }
        public string Currency { get; set; } = "VND";
        public DateTime AppliedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
