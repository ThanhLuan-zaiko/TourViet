using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TourViet.Models;

[Table("Payments")]
public class Payment
{
    [Key]
    public Guid PaymentID { get; set; } = Guid.NewGuid();

    [Required]
    public Guid BookingID { get; set; }

    [Required]
    [MaxLength(50)]
    public string PaymentMethod { get; set; } = "CreditCard";

    [Required]
    [Column(TypeName = "decimal(14,2)")]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(10)]
    public string Currency { get; set; } = "VND";

    [MaxLength(200)]
    public string? TransactionRef { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Completed";

    public DateTime? PaidAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("BookingID")]
    public virtual Booking Booking { get; set; } = null!;
}
