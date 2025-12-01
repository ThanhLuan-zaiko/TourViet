using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TourViet.Models
{
    public class Promotion
    {
        [Key]
        public Guid PromotionID { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(200)]
        public required string Name { get; set; }

        [StringLength(200)]
        public string? Slug { get; set; }

        public string? Description { get; set; }

        [Required]
        [StringLength(50)]
        public required string PromotionType { get; set; } // e.g., 'Coupon','Automatic','FlashSale'

        public DateTime? StartAt { get; set; }

        public DateTime? EndAt { get; set; }

        public bool IsActive { get; set; } = true;

        public int Priority { get; set; } = 100;

        public bool AllowStack { get; set; } = false;

        public int? MaxGlobalUses { get; set; }

        public int? MaxUsesPerUser { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MinTotalAmount { get; set; }

        public int? MinSeats { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<PromotionRule> PromotionRules { get; set; } = new List<PromotionRule>();
        public virtual ICollection<PromotionTarget> PromotionTargets { get; set; } = new List<PromotionTarget>();
        public virtual ICollection<Coupon> Coupons { get; set; } = new List<Coupon>();
        public virtual ICollection<PromotionRedemption> PromotionRedemptions { get; set; } = new List<PromotionRedemption>();
    }
}
