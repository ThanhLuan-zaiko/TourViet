using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TourViet.Models;

[Table("TourServices")]
public class TourService
{
    [Key]
    public Guid TourServiceID { get; set; } = Guid.NewGuid();

    [Required]
    public Guid TourID { get; set; }

    [Required]
    public Guid ServiceID { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? PriceOverride { get; set; }

    [MaxLength(10)]
    public string? Currency { get; set; }

    [Required]
    public bool IsIncluded { get; set; } = false;

    [Required]
    public int SortOrder { get; set; } = 0;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("TourID")]
    public virtual Tour Tour { get; set; } = null!;

    [ForeignKey("ServiceID")]
    public virtual Service Service { get; set; } = null!;
}