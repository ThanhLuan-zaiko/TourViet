using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TourViet.Models;

[Table("TourInstances")]
public class TourInstance
{
    [Key]
    public Guid InstanceID { get; set; } = Guid.NewGuid();

    [Required]
    public Guid TourID { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Required]
    [Range(0, int.MaxValue)]
    public int Capacity { get; set; }

    [Required]
    [Range(0, int.MaxValue)]
    public int SeatsBooked { get; set; } = 0;

    [Required]
    [Range(0, int.MaxValue)]
    public int SeatsHeld { get; set; } = 0;

    public DateTime? HoldExpires { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Open";

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal PriceBase { get; set; }

    [Required]
    [MaxLength(10)]
    public string Currency { get; set; } = "USD";

    public Guid? GuideID { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey("TourID")]
    public virtual Tour Tour { get; set; } = null!;

    [ForeignKey("GuideID")]
    public virtual User? Guide { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public virtual ICollection<TourPrice> TourPrices { get; set; } = new List<TourPrice>();
}