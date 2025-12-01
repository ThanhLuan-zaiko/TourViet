using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TourViet.Models
{
    public class PromotionRedemption
    {
        [Key]
        public Guid RedemptionID { get; set; } = Guid.NewGuid();

        [Required]
        public Guid PromotionID { get; set; }

        public Guid? CouponID { get; set; }

        public Guid? BookingID { get; set; }

        public Guid? UserID { get; set; }

        public Guid? InstanceID { get; set; }

        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }

        [Required]
        [StringLength(10)]
        public required string Currency { get; set; }

        [Required]
        [StringLength(50)]
        public required string Status { get; set; } = "Applied"; // Applied, Voided, Expired

        [StringLength(1000)]
        public string? Notes { get; set; }

        // Navigation properties
        [ForeignKey("PromotionID")]
        public virtual required Promotion Promotion { get; set; }

        [ForeignKey("CouponID")]
        public virtual Coupon? Coupon { get; set; }

        [ForeignKey("BookingID")]
        public virtual Booking? Booking { get; set; }

        [ForeignKey("UserID")]
        public virtual User? User { get; set; }
    }
}
