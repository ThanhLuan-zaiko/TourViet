using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TourViet.Models;

[Table("BookingServices")]
public class BookingService
{
    [Key]
    public Guid BookingServiceID { get; set; } = Guid.NewGuid();

    [Required]
    public Guid BookingID { get; set; }

    [Required]
    public Guid ServiceID { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; } = 1;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal PriceAtBooking { get; set; }

    [Required]
    [MaxLength(10)]
    public string Currency { get; set; } = "VND";

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("BookingID")]
    public virtual Booking Booking { get; set; } = null!;

    [ForeignKey("ServiceID")]
    public virtual Service Service { get; set; } = null!;
}
