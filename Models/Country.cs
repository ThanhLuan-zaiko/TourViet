using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TourViet.Models;

[Table("Countries")]
public class Country
{
    [Key]
    public Guid CountryID { get; set; } = Guid.NewGuid();

    [MaxLength(2)]
    public string? ISO2 { get; set; }

    [MaxLength(3)]
    public string? ISO3 { get; set; }

    [Required]
    [MaxLength(200)]
    public string CountryName { get; set; } = string.Empty;

    [MaxLength(10)]
    public string? CurrencyCode { get; set; }

    [MaxLength(20)]
    public string? PhoneCode { get; set; }

    [MaxLength(100)]
    public string? Timezone { get; set; }

    [MaxLength(100)]
    public string? Region { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<Location> Locations { get; set; } = new List<Location>();
}