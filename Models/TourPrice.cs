using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TourViet.Models;

[Table("TourPrices")]
public class TourPrice
{
    [Key]
    public Guid TourPriceID { get; set; } = Guid.NewGuid();

    public Guid? InstanceID { get; set; }

    public Guid? TourID { get; set; }

    [Required]
    [MaxLength(50)]
    public string PriceType { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(10)]
    public string Currency { get; set; } = "USD";

    // Navigation properties
    [ForeignKey("InstanceID")]
    public virtual TourInstance? TourInstance { get; set; }

    [ForeignKey("TourID")]
    public virtual Tour? Tour { get; set; }
}