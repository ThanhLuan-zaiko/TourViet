using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TourViet.Models
{
    public class PromotionRule
    {
        [Key]
        public Guid RuleID { get; set; } = Guid.NewGuid();

        [Required]
        public Guid PromotionID { get; set; }

        [Required]
        [StringLength(50)]
        public required string RuleType { get; set; } // 'Percent','Fixed','FreeSeat','BuyXGetY','FreeService'

        [Column(TypeName = "decimal(18,6)")]
        public decimal Value { get; set; } // percent as 10.00 = 10%, or fixed amount

        [StringLength(10)]
        public string? Currency { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MaxDiscountAmount { get; set; }

        [StringLength(50)]
        public string? AppliesToSeatType { get; set; }

        public string? Conditions { get; set; }

        // Navigation properties
        [ForeignKey("PromotionID")]
        public virtual required Promotion Promotion { get; set; }
    }
}
