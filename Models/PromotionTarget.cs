using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TourViet.Models
{
    public class PromotionTarget
    {
        [Key]
        public Guid PromotionTargetID { get; set; } = Guid.NewGuid();

        [Required]
        public Guid PromotionID { get; set; }

        [Required]
        [StringLength(50)]
        public required string TargetType { get; set; } // 'All','Tour','Instance','Category'

        public Guid? TargetID { get; set; } // NULL if TargetType = 'All'

        // Navigation properties
        [ForeignKey("PromotionID")]
        public virtual required Promotion Promotion { get; set; }
        
        // Optional navigation to Tour (when TargetType = 'Tour')
        [ForeignKey("TargetID")]
        public virtual Tour? Tour { get; set; }
    }
}
