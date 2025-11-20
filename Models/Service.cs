using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TourViet.Models;

[Table("Services")]
public class Service
{
    [Key]
    public Guid ServiceID { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(200)]
    public string ServiceName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Code { get; set; }

    public string? Description { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; } = 0.00m;

    [Required]
    [MaxLength(10)]
    public string Currency { get; set; } = "USD";

    [Required]
    public bool IsActive { get; set; } = true;

    [Required]
    public bool IsTaxable { get; set; } = true;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [Required]
    public bool IsDeleted { get; set; } = false;

    // Navigation properties
    public virtual ICollection<TourService> TourServices { get; set; } = new List<TourService>();
}