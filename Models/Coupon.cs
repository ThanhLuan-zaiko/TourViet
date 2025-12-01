using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TourViet.Models
{
    public class Coupon
    {
        [Key]
        public Guid CouponID { get; set; } = Guid.NewGuid();

        [Required]
        public Guid PromotionID { get; set; }

        [Required]
        [StringLength(100)]
        public required string Code { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? StartsAt { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public int? MaxUses { get; set; }

        public int? MaxUsesPerUser { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("PromotionID")]
        public virtual required Promotion Promotion { get; set; }
        public virtual ICollection<PromotionRedemption> PromotionRedemptions { get; set; } = new List<PromotionRedemption>();
    }
}
