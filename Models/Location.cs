using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TourViet.Models;

[Table("Locations")]
public class Location
{
    [Key]
    public Guid LocationID { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(200)]
    public string LocationName { get; set; } = string.Empty;

    public Guid? CountryID { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public string? Description { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [Required]
    public bool IsDeleted { get; set; } = false;

    // Navigation properties
    [ForeignKey("CountryID")]
    public virtual Country? Country { get; set; }

    public virtual ICollection<Tour> Tours { get; set; } = new List<Tour>();
}