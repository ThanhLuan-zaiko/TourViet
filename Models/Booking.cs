using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TourViet.Models;

[Table("Bookings")]
public class Booking
{
    [Key]
    public Guid BookingID { get; set; } = Guid.NewGuid();

    [Required]
    public Guid InstanceID { get; set; }

    [Required]
    public Guid UserID { get; set; }

    [Required]
    [MaxLength(50)]
    public string BookingRef { get; set; } = string.Empty;

    [Required]
    [Range(1, int.MaxValue)]
    public int Seats { get; set; }

    [Required]
    [Column(TypeName = "decimal(14,2)")]
    public decimal TotalAmount { get; set; }

    [Required]
    [MaxLength(10)]
    public string Currency { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Pending";

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey("InstanceID")]
    public virtual TourInstance TourInstance { get; set; } = null!;

    [ForeignKey("UserID")]
    public virtual User User { get; set; } = null!;
}